using System.Globalization;
using System.Threading.Tasks;

namespace FoodTour.Mobile;

public partial class App : Application
{
    /// <summary>
    /// Mã định danh thiết bị duy nhất, được tạo một lần và lưu bền vững vào SQLite.
    /// Không bị mất khi user xóa cache hay clear Preferences.
    /// Dùng để đồng bộ lên Web Admin qua API /api/device/sync.
    /// </summary>
    public static string DeviceId { get; private set; } = string.Empty;

    /// <summary>Tên model máy (DeviceInfo.Model), lưu cùng lúc với DeviceId.</summary>
    public static string DeviceName { get; private set; } = string.Empty;

    // Quản lý tiến trình gửi Heartbeat ngầm
    private CancellationTokenSource? _heartbeatCts;
    private Services.DatabaseService? _databaseService;

    private Page _initialPage = new Views.SplashPage();

    private readonly ViewModels.PlayerViewModel _playerVm;
    private readonly Services.WalkingSimulationService _locationService;

    public App(Services.ILocalizationService localizationService, Services.DatabaseService databaseService, ViewModels.PlayerViewModel playerVm, Services.WalkingSimulationService locationService)
    {
        InitializeComponent();
        _playerVm = playerVm;
        _locationService = locationService;

        InitializeAppAsync(localizationService, databaseService);
    }

    private async void InitializeAppAsync(Services.ILocalizationService localizationService, Services.DatabaseService databaseService)
    {
        _databaseService = databaseService;

        // ── Khởi tạo DeviceID từ SQLite (bền vững, không mất khi clear Preferences) ──
        DeviceId = await databaseService.GetOrCreateDeviceIdAsync();
        // Đọc lại DeviceName từ bản ghi đã lưu để dùng khi sync lên API
        DeviceName = DeviceInfo.Model ?? "Unknown Device";
        
        // Gọi lên Backend kiểm tra trạng thái khóa. Await để chắc chắn có kết quả trước khi tiếp tục.
        bool isBlocked = await databaseService.SyncDeviceToServerAsync(DeviceId, DeviceName);

        if (isBlocked)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var blockedPage = new Views.BlockedPage();
                if (Application.Current?.Windows.Count > 0)
                {
                    Application.Current.Windows[0].Page = blockedPage;
                }
                else
                {
                    _initialPage = blockedPage;
                }
            });
            // Dừng hoàn toàn quá trình khởi tạo ứng dụng và localization
            return;
        }
        // ────────────────────────────────────────────────────────────────────────────

        // Auto-Detect & Auto-Translate Logic
        // Gọi xuống hệ điều hành máy để hỏi xem máy đang cài ngôn ngữ gì 
        var currentLang = Preferences.Default.Get("AppLanguage", string.Empty);

        if (string.IsNullOrEmpty(currentLang))
        {
            var osLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var supportedLangs = new[] { "vi", "en", "ja", "ru", "zh" };

            if (Array.Exists(supportedLangs, lang => lang == osLang))
            {
                currentLang = osLang;
            }
            else
            {
                currentLang = "en"; // Fallback
            }
            Preferences.Default.Set("AppLanguage", currentLang);
        }

        // Determine if we should wait for OTA localization
        var isOfflineMode = Preferences.Default.Get("IsOfflineMode", false);

        Task locTask;
        if (isOfflineMode)
        {
            // In offline mode, just trigger the change and move on (service uses cache)
            locTask = localizationService.ChangeLanguageAsync(currentLang);
        }
        else
        {
            // In online mode, wait for it (blocks for better UX if server is up)
            locTask = localizationService.ChangeLanguageAsync(currentLang);
        }

        var delayTask = Task.Delay(isOfflineMode ? 500 : 3000); // Shorter splash for offline
        await Task.WhenAll(locTask, delayTask);

        // Safe Routing
        var isSetupCompleted = Preferences.Default.Get("IsSetupCompleted", false);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Page newPage = isSetupCompleted
                ? new AppShell(_playerVm, _locationService, localizationService, databaseService)
                : new NavigationPage(new Views.OnboardingPage(localizationService, databaseService, _playerVm, _locationService));

            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = newPage;
            }
            else
            {
                _initialPage = newPage;
            }
        });
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(_initialPage);

        // Đăng ký sự kiện Vòng đời của Window để quản lý Heartbeat
        window.Activated += (s, e) => StartHeartbeat();
        window.Deactivated += (s, e) => StopHeartbeat();

        return window;
    }

    // ═══════ HEARTBEAT (ONLINE / OFFLINE) ═══════

    private void StartHeartbeat()
    {
        StopHeartbeat(); // Đảm bảo không chạy nhiều vòng lặp trùng nhau
        _heartbeatCts = new CancellationTokenSource();
        _ = RunHeartbeatLoopAsync(_heartbeatCts.Token);
    }

    private void StopHeartbeat()
    {
        _heartbeatCts?.Cancel();
        _heartbeatCts?.Dispose();
        _heartbeatCts = null;
    }

    private async Task RunHeartbeatLoopAsync(CancellationToken token)
    {
        try
        {
            // Sử dụng PeriodicTimer giúp quản lý chu kỳ lặp tối ưu và không bị giật lag
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
            
            // Lặp ngầm mỗi phút cho đến khi token bị hủy (App xuống nền / đóng)
            while (await timer.WaitForNextTickAsync(token))
            {
                if (_databaseService != null && !string.IsNullOrEmpty(DeviceId))
                {
                    bool isBlocked = await _databaseService.SyncDeviceToServerAsync(DeviceId, DeviceName);
                    
                    // Nếu phát hiện thiết bị vừa bị khóa từ phiên Heartbeat
                    if (isBlocked)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page is not Views.BlockedPage)
                            {
                                Application.Current.Windows[0].Page = new Views.BlockedPage();
                            }
                        });
                        
                        // Khóa xong rồi thì không cần đập Heartbeat nữa
                        StopHeartbeat(); 
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ngoại lệ bình thường xảy ra khi Cancel token -> App đi vào Background
            System.Diagnostics.Debug.WriteLine("[Heartbeat] Đã tạm dừng do App xuống nền.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Heartbeat] Lỗi: {ex.Message}");
        }
    }
}