using System.Globalization;
using System.Text.RegularExpressions;
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
    private Services.ILocalizationService? _localizationService;

    // ── Deep Link: Lưu URI chờ xử lý khi app chưa khởi tạo xong ──
    private Uri? _pendingDeepLinkUri;
    private bool _isAppReady;

    // Regex trích xuất GUID (shopId) từ URL Deep Link
    private static readonly Regex GuidRegex = new(
        @"([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
        RegexOptions.Compiled);

    public App(
        Services.ILocalizationService localizationService, 
        Services.DatabaseService databaseService, 
        ViewModels.PlayerViewModel playerVm, 
        Services.WalkingSimulationService locationService, 
        Services.IHardwareIdService hardwareIdService)
    {
        InitializeComponent();
        _playerVm = playerVm;
        _locationService = locationService; 
        _localizationService = localizationService;

        // Gán ngay DeviceId bằng HardwareId thay vì chờ DB
        DeviceId = hardwareIdService.GetHardwareId();
        DeviceName = DeviceInfo.Model ?? "Unknown Device";

        InitializeAppAsync(localizationService, databaseService);
    }

    private async void InitializeAppAsync(Services.ILocalizationService localizationService, Services.DatabaseService databaseService)
    {
        _databaseService = databaseService;

        // Đẩy lên Backend nền — không await để không block splash screen
        _ = databaseService.SyncDeviceToServerAsync(DeviceId, DeviceName);
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

            // ── Đánh dấu app đã sẵn sàng, xử lý Deep Link đang chờ nếu có ──
            _isAppReady = true;
            if (_pendingDeepLinkUri != null)
            {
                var uri = _pendingDeepLinkUri;
                _pendingDeepLinkUri = null;
                _ = HandleDeepLinkAsync(uri);
            }
        });
    }

    // ═══════ DEEP LINK HANDLING ═══════

    /// <summary>
    /// Được gọi từ MainActivity (Android) khi nhận Intent chứa Deep Link URI.
    /// Nếu app chưa khởi tạo xong, URI sẽ được lưu tạm để xử lý sau.
    /// </summary>
    public void SendDeepLink(Uri uri)
    {
        System.Diagnostics.Debug.WriteLine($"[DeepLink] SendDeepLink: {uri}");

        if (_isAppReady)
        {
            // App đã sẵn sàng — xử lý ngay
            _ = HandleDeepLinkAsync(uri);
        }
        else
        {
            // App đang splash/loading — lưu tạm, xử lý khi InitializeAppAsync xong
            _pendingDeepLinkUri = uri;
        }
    }

    /// <summary>
    /// Xử lý logic Deep Link chính:
    /// 1. Trích xuất shopId từ URI
    /// 2. Lấy Hardware ID thực tế của thiết bị
    /// 3. Gọi API kiểm tra trạng thái Premium
    /// 4. Điều hướng vào ShopDetailPage kèm thông tin Premium/Trial
    /// </summary>
    private async Task HandleDeepLinkAsync(Uri uri)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[DeepLink] Đang xử lý URI: {uri}");

            // ── 1. Trích xuất ShopId từ path: /foodtour/{shopId} ──
            string? shopId = ExtractShopIdFromUri(uri);
            if (string.IsNullOrEmpty(shopId))
            {
                System.Diagnostics.Debug.WriteLine("[DeepLink] Không tìm thấy ShopId trong URI.");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[DeepLink] ShopId: {shopId}");

            // ── 2. Lấy Hardware ID thực tế (AndroidId) ──
            string hardwareId = GetHardwareId();
            System.Diagnostics.Debug.WriteLine($"[DeepLink] HardwareId: {hardwareId}");

            // ── 3. Gọi API kiểm tra trạng thái Premium ──
            bool isPremium = false;
            int trialRemaining = 3;

            if (_databaseService != null)
            {
                var status = await _databaseService.CheckDeviceStatusAsync(hardwareId);
                if (status != null)
                {
                    isPremium = status.IsPremium;
                    trialRemaining = status.TrialRemaining;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[DeepLink] Premium: {isPremium}, TrialRemaining: {trialRemaining}");

            // ── 4. Điều hướng vào ShopDetailPage kèm thông tin ──
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    // Truyền dữ liệu qua Query Parameters cho Shell Navigation
                    var navigationParams = new Dictionary<string, object>
                    {
                        { "ShopId", shopId },
                        { "IsFromDeepLink", true },
                        { "IsPremium", isPremium },
                        { "TrialRemaining", trialRemaining },
                        { "HardwareId", hardwareId }
                    };

                    // FIX: Delay 800ms để đảm bảo MAUI Shell đã khởi tạo xong
                    // UI Tree / Navigation Stack ở COLD Boot.
                    // Ngăn lỗi Race Condition gây trắng màn hình và mất FloatingAudioPlayer.
                    await Task.Delay(800);

                    await Shell.Current.GoToAsync(
                        nameof(Views.ShopDetailPage),
                        navigationParams);

                    System.Diagnostics.Debug.WriteLine("[DeepLink] Đã điều hướng vào ShopDetailPage.");
                }
                catch (Exception navEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[DeepLink] Lỗi điều hướng: {navEx.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DeepLink] Lỗi xử lý: {ex.Message}");
        }
    }

    /// <summary>
    /// Trích xuất Shop ID (GUID) từ URI Deep Link.
    /// Hỗ trợ format: /foodtour/{guid}
    /// </summary>
    private static string? ExtractShopIdFromUri(Uri uri)
    {
        // Thử lấy từ path segments: /foodtour/{shopId}
        var path = uri.AbsolutePath.TrimEnd('/');
        if (path.StartsWith("/foodtour/", StringComparison.OrdinalIgnoreCase))
        {
            var shopId = path.Substring("/foodtour/".Length);
            if (!string.IsNullOrEmpty(shopId))
                return shopId;
        }

        // Fallback: dùng Regex tìm GUID bất kỳ trong URL
        var match = GuidRegex.Match(uri.ToString());
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Lấy Hardware ID — ưu tiên dùng IHardwareIdService (Android-specific),
    /// fallback về DeviceId từ SQLite nếu service không khả dụng.
    /// </summary>
    private string GetHardwareId()
    {
        return DeviceId;
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
                    await _databaseService.SyncDeviceToServerAsync(DeviceId, DeviceName);
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