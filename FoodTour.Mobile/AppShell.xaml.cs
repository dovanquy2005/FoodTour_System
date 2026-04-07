using FoodTour.Mobile.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Networking;
using Microsoft.Maui.ApplicationModel;
using FoodTour.Mobile.Messages;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AspNetCore.SignalR.Client;

namespace FoodTour.Mobile;

public partial class AppShell : Shell
{
    private readonly Services.ILocalizationService _localizationService;
    private readonly Services.DatabaseService _databaseService;
    private readonly Services.WalkingSimulationService _locationService;
    // Chống chạy đồng thời: nếu lần trước chưa xong thì bỏ qua lần này
    private readonly SemaphoreSlim _pollingLock = new SemaphoreSlim(1, 1);
    // Kết nối SignalR tới Backend Hub để nhận thông báo cập nhật thời gian thực
    private HubConnection? _hubConnection;

    public AppShell(ViewModels.PlayerViewModel playerVm, Services.WalkingSimulationService locationService, Services.ILocalizationService localizationService, Services.DatabaseService databaseService)
    {
        InitializeComponent();
        BindingContext = playerVm;
        _localizationService = localizationService;
        _databaseService = databaseService;
        _locationService = locationService;

        // Đăng ký route cho trang chi tiết
        Routing.RegisterRoute(nameof(Views.ShopDetailPage), typeof(Views.ShopDetailPage));
        Routing.RegisterRoute(nameof(Views.LanguageSelectionPage), typeof(Views.LanguageSelectionPage));

        // Đăng ký sự kiện lắng nghe kết nối mạng
        Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;

        // Đăng ký nhận thông báo DataSyncedMessage (sau khi WiFi auto-sync ngầm thành công)
        WeakReferenceMessenger.Default.Register<DataSyncedMessage>(this, async (r, m) =>
        {
            await ShowSnackbarAsync(_localizationService["Notify_SyncComplete"] ?? "Dữ liệu bản đồ đã được cập nhật mới nhất!");
        });

        // Đăng ký nhận thông báo NewUpdateAvailableMessage (có update khi dùng 4G)
        WeakReferenceMessenger.Default.Register<NewUpdateAvailableMessage>(this, async (r, m) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ShowAlertBadge();
            });
            await ShowUpdateSnackbarAsync("Có bản cập nhật mới, vui lòng kiểm tra!");
        });

    }

    /// <summary>
    /// Khởi tạo kết nối SignalR tới Backend Hub.
    /// Khi Server broadcast sự kiện "ReceiveUpdate" (admin sửa shop),
    /// app sẽ tự động đồng bộ dữ liệu mới ngay lập tức.
    /// </summary>
    private async Task InitSignalRAsync()
    {
        try
        {
            // Xây dựng kết nối SignalR với cơ chế tự động kết nối lại khi mất mạng
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{AppConfig.ApiBaseUrl}/api/updatesHub")
                .WithAutomaticReconnect(new[]
                {
                    // Chiến lược reconnect: thử lại sau 0s, 2s, 5s, 10s, 30s
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30)
                })
                .Build();

            // Lắng nghe sự kiện "ReceiveUpdate" từ Server
            // Khi admin thay đổi Shop (text, audio, radius...), server sẽ gửi shopId vừa thay đổi
            _hubConnection.On<string>("ReceiveUpdate", async (shopId) =>
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Nhận tín hiệu cập nhật cho Shop: {shopId}");
                await HandleServerUpdateAsync();
            });

            // Ghi log khi kết nối lại thành công sau khi mất mạng
            _hubConnection.Reconnected += async (connectionId) =>
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Đã kết nối lại: {connectionId}");
                // Sau khi reconnect, đồng bộ lại dữ liệu để bù các thay đổi bị mất trong lúc mất kết nối
                await HandleServerUpdateAsync();
            };

            _hubConnection.Closed += (error) =>
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Mất kết nối: {error?.Message}");
                return Task.CompletedTask;
            };

            // Bắt đầu kết nối — nếu server chưa sẵn sàng (cold start), thử lại sau 5s
            await StartHubConnectionWithRetryAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SignalR] Lỗi khởi tạo: {ex.Message}");
            // Fallback: nếu SignalR không kết nối được, dùng Timer dự phòng
            StartFallbackPollingTimer();
        }
    }

    /// <summary>
    /// Thử kết nối SignalR với retry, tránh crash nếu server đang ngủ (Render cold start).
    /// </summary>
    private async Task StartHubConnectionWithRetryAsync()
    {
        for (int attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                if (_hubConnection?.State == HubConnectionState.Disconnected)
                {
                    await _hubConnection.StartAsync();
                    System.Diagnostics.Debug.WriteLine($"[SignalR] Kết nối thành công! (lần thử {attempt})");
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Thử kết nối lần {attempt}/5 thất bại: {ex.Message}");
                if (attempt < 5)
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // 2s, 4s, 8s, 16s
            }
        }

        // Sau 5 lần thất bại, chuyển sang polling dự phòng
        System.Diagnostics.Debug.WriteLine("[SignalR] Không thể kết nối sau 5 lần, sử dụng Timer dự phòng.");
        StartFallbackPollingTimer();
    }

    /// <summary>
    /// Xử lý khi nhận tín hiệu cập nhật từ Server qua SignalR.
    /// Logic giữ nguyên: WiFi → sync ngầm, 4G → thông báo cho user chọn tải.
    /// </summary>
    private async Task HandleServerUpdateAsync()
    {
        // Chống chạy đồng thời: nếu lần trước chưa xong thì bỏ qua
        if (!_pollingLock.Wait(0)) return;

        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;

            bool isWiFi = Helpers.NetworkHelper.IsFreeNetwork();

            if (isWiFi)
            {
                // WiFi: đồng bộ ngầm tự động, không hỏi user
                bool synced = await _databaseService.SyncDataFromApiAsync(AppConfig.ApiBaseUrl);
                if (synced)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        WeakReferenceMessenger.Default.Send(new DataSyncedMessage("synced"));
                    });
                }
            }
            else
            {
                // Mạng di động: chỉ kiểm tra có update, thông báo cho user chọn tải
                bool hasUpdate = await _databaseService.CheckForUpdatesAsync();
                if (hasUpdate)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        WeakReferenceMessenger.Default.Send(new NewUpdateAvailableMessage(0, 0));
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SignalR] Lỗi xử lý cập nhật: {ex.Message}");
        }
        finally
        {
            _pollingLock.Release();
        }
    }

    /// <summary>
    /// Timer dự phòng khi SignalR không kết nối được (ví dụ server offline).
    /// Quét mỗi 30 giây thay vì 5 giây để tiết kiệm pin hơn.
    /// </summary>
    private void StartFallbackPollingTimer()
    {
        Application.Current!.Dispatcher.StartTimer(TimeSpan.FromSeconds(30), () =>
        {
            _ = HandleServerUpdateAsync();
            return true; // Tiếp tục chạy timer
        });
    }

    private async Task ShowSnackbarAsync(string message)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var snackbarOptions = new CommunityToolkit.Maui.Core.SnackbarOptions
            {
                BackgroundColor = Color.FromArgb("#4CAF50"), // Bright green for success
                TextColor = Colors.White,
                ActionButtonTextColor = Colors.White,
                CornerRadius = new CornerRadius(14),
                CharacterSpacing = 0.0,
                Font = Microsoft.Maui.Font.SystemFontOfSize(14)
            };
            var snackbar = CommunityToolkit.Maui.Alerts.Snackbar.Make(
                "✨ " + message,
                visualOptions: snackbarOptions,
                duration: TimeSpan.FromSeconds(4)
            );
            await snackbar.Show();
        });
    }

    // Lưu thời gian mở popup 4G để tránh spam khi timer 5s quét liên tục
    private DateTime _lastUpdateSnackbarTime = DateTime.MinValue;

    private async Task ShowUpdateSnackbarAsync(string message)
    {
        // Throttling: không hiện lại nếu chưa quá 10 giây (tránh spam với timer 5s)
        if ((DateTime.Now - _lastUpdateSnackbarTime).TotalSeconds < 10) return;
        _lastUpdateSnackbarTime = DateTime.Now;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var snackbarOptions = new CommunityToolkit.Maui.Core.SnackbarOptions
            {
                BackgroundColor = Color.FromArgb("#FF9800"), // Vibrant orange
                TextColor = Colors.White,
                ActionButtonTextColor = Colors.White,
                CornerRadius = new CornerRadius(14),
                CharacterSpacing = 0.0,
                Font = Microsoft.Maui.Font.SystemFontOfSize(15)
            };
            var snackbar = CommunityToolkit.Maui.Alerts.Snackbar.Make(
                "🚀 " + message,
                actionButtonText: _localizationService["Common_Yes"] ?? "TẢI NGAY",
                action: async () =>
                {
                    var dbService = Application.Current?.Handler?.MauiContext?.Services.GetService<Services.DatabaseService>();
                    if (dbService == null) return;

                    var list = await dbService.GetNotificationsAsync();
                    var latest = list.FirstOrDefault(n => n.Status == "Available" || n.Status == "Error");
                    if (latest != null)
                    {
                        string alertMsg = string.Format(
                            _localizationService["Notify_MobileDataConfirm"] ?? "Có dữ liệu POI mới ({0}), bạn có muốn cập nhật bằng mạng di động không?",
                            latest.SizeDisplay);

                        bool confirm = await Shell.Current.DisplayAlert(
                            _localizationService["Alert_Title"] ?? "Thông báo",
                            alertMsg,
                            _localizationService["Common_Yes"] ?? "Có",
                            _localizationService["Common_No"] ?? "Không"
                        );

                        if (confirm)
                        {
                            bool success = await dbService.DownloadUpdateAsync(latest);
                            if (success) {
                                WeakReferenceMessenger.Default.Send(new Messages.DataSyncedMessage("synced"));
                                await ShowSnackbarAsync(_localizationService["Notify_SyncComplete"] ?? "Dữ liệu bản đồ đã được cập nhật mới nhất!");
                            } else {
                                await Shell.Current.DisplayAlert("Lỗi", "Tải xuống thất bại", "OK");
                            }
                        }
                    }
                },
                visualOptions: snackbarOptions,
                duration: TimeSpan.FromSeconds(7)
            );
            await snackbar.Show();
        });
    }

    private void ShowAlertBadge()
    {
        // Hiển thị một chấm đỏ (bằng cách thêm dấu chấm vào Title do MAUI chưa support native Badge đầy đủ trên mọi nền tảng)
        var baseTitle = _localizationService["Tab_Alerts"] ?? "Thông báo";
        if (AlertsTab.Title != null && !AlertsTab.Title.EndsWith("•"))
        {
            AlertsTab.Title = $"{baseTitle} •";
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateStatusIndicator();
        
        // Bắt đầu dịch vụ GPS ngầm toàn cục (Đã di chuyển từ constructor để tránh lỗi Activity)
        _ = _locationService.Start();

        // Khởi tạo SignalR nếu chưa có
        if (_hubConnection == null)
        {
            _ = InitSignalRAsync();
        }
    }

    private void Connectivity_ConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateStatusIndicator();
        });

        // Khi mạng hồi phục, thử kết nối lại SignalR nếu đang bị ngắt
        if (e.NetworkAccess == NetworkAccess.Internet && _hubConnection?.State == HubConnectionState.Disconnected)
        {
            _ = StartHubConnectionWithRetryAsync();
        }
    }

    // Cập nhật chỉ báo trạng thái mạng — đã loại bỏ logic "Offline Mode" cũ
    // Chỉ hiển thị trạng thái kết nối mạng thực tế
    public void UpdateStatusIndicator()
    {
        var currentAccess = Connectivity.Current.NetworkAccess;

        if (currentAccess == NetworkAccess.Internet)
        {
            // Có kết nối mạng → ẩn chỉ báo
            StatusIndicatorContainer.IsVisible = false;
        }
        else
        {
            // Không có mạng → hiển thị cảnh báo mất kết nối
            StatusIcon.Text = "❌";
            StatusText.Text = "No Connection";
            StatusText.TextColor = Colors.Red;
            StatusIndicatorContainer.IsVisible = true;
        }
    }

    ~AppShell()
    {
        Connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;
        // Đóng kết nối SignalR khi hủy AppShell
        _ = _hubConnection?.DisposeAsync();
    }
}
