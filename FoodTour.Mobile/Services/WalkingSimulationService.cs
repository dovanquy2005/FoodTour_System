using Microsoft.Maui.Devices.Sensors;
using FoodTour.Mobile.Models;
using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.Messaging;
using FoodTour.Mobile.Messages;

namespace FoodTour.Mobile.Services;

public class WalkingSimulationService : IRecipient<LanguageChangedMessage>, IRecipient<AudioFilesUpdatedMessage>, IDisposable
{
    private readonly DatabaseService _dbService;
    private readonly IAudioPlayerService _audioService;
    private List<ShopModel> _shops = new();
    private ShopModel? _currentShop = null; // Shop đang active (đang phát audio)
    private bool _isRunning = false;
    public Action<Location>? OnLocationUpdate;
    public Action? OnRouteFinished;
    public event Action<ShopModel>? ShopEntered;
    public event Action<ShopModel>? ShopExited;

    private Location? _routeEnd;
    private DateTime _startTime;
    private Location? _lastKnownLocation; // Lưu vị trí cuối cùng để dùng khi cần check lại

    // Chống spam: Debounce + Cooldown
    private DateTime _lastCheckTime = DateTime.MinValue;
    private const int DebounceMs = 2_000; // ms tối thiểu giữa 2 lần check liên tiếp
    private readonly ConcurrentDictionary<string, DateTime> _shopCooldowns = new();
    private const int CooldownMinutes = 1;
    private const double DefaultActivationRadiusM = 50.0;
    private const double HysteresisMultiplier = 1.3;

    // ═══════ HÀNG ĐỢI AUDIO TUẦN TỰ (Sequential Priority Queue) ═══════
    // Danh sách các shop đang chờ phát audio, sắp xếp theo Priority giảm dần
    private readonly List<ShopModel> _audioQueue = new();
    // Cờ đánh dấu đang xử lý phát audio tuần tự (tránh chạy đồng thời)
    private bool _isProcessingQueue = false;

    public WalkingSimulationService(DatabaseService dbService, IAudioPlayerService audioService)
    {
        _dbService = dbService;
        _audioService = audioService;

        // Đăng ký nhận sự kiện đổi ngôn ngữ toàn cục
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this);
        // Đăng ký nhận sự kiện khi file audio mới được tải xuống đĩa
        WeakReferenceMessenger.Default.Register<AudioFilesUpdatedMessage>(this);

        // ═══════ ĐĂNG KÝ LẮNG NGHE KHI AUDIO PHÁT XONG ═══════
        // Thay vì dùng StateChanged và phụ thuộc text dịch, ta dùng sự kiện PlaybackEnded chuẩn xác
        _audioService.PlaybackEnded += OnPlaybackEnded;
    }

    public void SetRouteEnd(Location end) => _routeEnd = end;

    public async Task Start()
    {
        if (_isRunning) return;

        try
        {
            // Reload shops mỗi khi start để cập nhật Priority/Radius mới nhất
            var data = await _dbService.GetShopsAsync();
            _shops = data.ToList();

            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted) return;

            Geolocation.Default.LocationChanged -= OnLocationChanged;
            Geolocation.Default.LocationChanged += OnLocationChanged;

            if (!Geolocation.Default.IsListeningForeground)
            {
                var request = new GeolocationListeningRequest(
                    GeolocationAccuracy.Best,
                    TimeSpan.FromSeconds(1));

                await Geolocation.Default.StartListeningForegroundAsync(request);
            }

            _startTime = DateTime.UtcNow;
            _isRunning = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GeoFence] Start error: {ex.Message}");
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _audioService.Stop();

        // Xóa hàng đợi khi dừng dịch vụ
        lock (_audioQueue)
        {
            _audioQueue.Clear();
        }

        try
        {
            Geolocation.Default.LocationChanged -= OnLocationChanged;

            if (Geolocation.Default.IsListeningForeground)
                Geolocation.Default.StopListeningForeground();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GeoFence] Stop error: {ex.Message}");
        }
    }

    private void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        if (!_isRunning || e.Location is null) return;

        // Bỏ qua GPS kém và dữ liệu vị trí cũ sát với lúc bật (chống tự phát khi mới mở app)
        if (e.Location.Accuracy.HasValue && e.Location.Accuracy.Value > 25.0) return;
        if ((DateTime.UtcNow - _startTime).TotalSeconds < 2) return;

        // Lưu vị trí cuối cùng để dùng khi audio phát xong → check tiếp shop kế
        _lastKnownLocation = e.Location;

        OnLocationUpdate?.Invoke(e.Location);

        _ = CheckShopAsync(e.Location); // fire-and-forget, non-blocking
        CheckEnd(e.Location);
    }

    /// <summary>
    /// Kiểm tra vị trí người dùng với tất cả shop.
    /// Logic mới: Tìm TẤT CẢ shop trong bán kính → đẩy vào hàng đợi theo Priority → phát tuần tự.
    /// </summary>
    private async Task CheckShopAsync(Location userLocation)
    {
        // ── 1. DEBOUNCE ───────────────────────────────────────────────────────
        var now = DateTime.UtcNow;
        if ((now - _lastCheckTime).TotalMilliseconds < DebounceMs) return;
        _lastCheckTime = now;

        // ── 2. HYSTERESIS EXIT CHECK ──────────────────────────────────────────
        // Kiểm tra xem user đã RỜI KHỎI shop đang active chưa (dùng bán kính mở rộng)
        if (_currentShop is not null)
        {
            double exitRadius = GetActivationRadius(_currentShop) * HysteresisMultiplier;
            double distToCurrent = MetersTo(_currentShop, userLocation);

            if (distToCurrent <= exitRadius)
            {
                // Vẫn trong vùng mở rộng → nếu đang phát thì không làm gì,
                // nếu audio đã phát xong thì để OnAudioStateChanged xử lý chuyển shop tiếp
                if (_audioService.IsPlaying)
                    return;
                // Audio đã kết thúc nhưng vẫn trong vùng → chờ OnAudioStateChanged xử lý
                return;
            }

            // User đã THỰC SỰ rời khỏi shop (vượt quá bán kính hysteresis)
            Console.WriteLine($"[GeoFence] Exited: {_currentShop.Name}");
            var exitedShop = _currentShop;
            _currentShop = null;
            
            MainThread.BeginInvokeOnMainThread(() => ShopExited?.Invoke(exitedShop));

            // Xóa shop vừa thoát ra khỏi hàng đợi (nếu có)
            lock (_audioQueue)
            {
                _audioQueue.RemoveAll(s => s.Id == exitedShop.Id);
            }

            // Fall through: có thể shop khác đang ngay bên cạnh
        }

        // ── 3. TÌM TẤT CẢ SHOP TRONG BÁN KÍNH (Vùng giao thoa) ────────────
        var candidates = _shops
            .Select(s => (shop: s, dist: MetersTo(s, userLocation), radius: GetActivationRadius(s)))
            .Where(x => x.dist <= x.radius)               // trong vùng kích hoạt
            .OrderByDescending(x => x.shop.Priority)       // ưu tiên cao trước
            .ThenBy(x => x.dist)                           // gần nhất là tiebreaker
            .ToList();

        if (candidates.Count == 0) return;

        // ── 4. ĐẨY TẤT CẢ ỨNG CỬ VIÊN VÀO HÀNG ĐỢI ──────────────────────
        lock (_audioQueue)
        {
            foreach (var candidate in candidates)
            {
                // Bỏ qua shop đang trong cooldown
                if (_shopCooldowns.TryGetValue(candidate.shop.Id, out var lastPlayed)
                    && (now - lastPlayed).TotalMinutes < CooldownMinutes)
                {
                    Console.WriteLine($"[GeoFence] Cooldown active: {candidate.shop.Name} " +
                                      $"({(int)(now - lastPlayed).TotalSeconds}s / {CooldownMinutes * 60}s)");
                    continue;
                }

                // Bỏ qua shop đã có trong hàng đợi hoặc đang phát
                if (_audioQueue.Any(s => s.Id == candidate.shop.Id)) continue;
                if (_currentShop?.Id == candidate.shop.Id) continue;

                _audioQueue.Add(candidate.shop);
                Console.WriteLine($"[GeoFence] Thêm vào hàng đợi: {candidate.shop.Name} (priority={candidate.shop.Priority})");
            }

            // Sắp xếp lại hàng đợi theo Priority giảm dần
            _audioQueue.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        // ── 5. BẮT ĐẦU PHÁT TUẦN TỰ NẾU CHƯA CÓ AUDIO NÀO ĐANG CHẠY ─────
        if (_currentShop == null && !_audioService.IsPlaying)
        {
            await ProcessAudioQueueAsync();
        }
    }

    /// <summary>
    /// Xử lý hàng đợi audio: lấy shop ưu tiên cao nhất ra phát.
    /// Được gọi khi:
    /// - User lọt vào vùng shop mới và chưa có audio đang phát
    /// - Audio trước đó phát xong (qua OnAudioStateChanged)
    /// </summary>
    private async Task ProcessAudioQueueAsync()
    {
        // Chống chạy đồng thời
        if (_isProcessingQueue) return;
        _isProcessingQueue = true;

        try
        {
            ShopModel? nextShop = null;

            lock (_audioQueue)
            {
                if (_audioQueue.Count == 0)
                {
                    _isProcessingQueue = false;
                    return;
                }

                // Lấy shop ưu tiên cao nhất (đầu danh sách — đã sắp xếp)
                nextShop = _audioQueue[0];
                _audioQueue.RemoveAt(0);
            }

            if (nextShop == null)
            {
                _isProcessingQueue = false;
                return;
            }

            // Kiểm tra user vẫn còn trong bán kính shop này (tránh phát cho shop đã rời)
            if (_lastKnownLocation != null)
            {
                double dist = MetersTo(nextShop, _lastKnownLocation);
                double radius = GetActivationRadius(nextShop);
                if (dist > radius * HysteresisMultiplier)
                {
                    Console.WriteLine($"[GeoFence] Bỏ qua {nextShop.Name}: user đã rời khỏi bán kính.");
                    _isProcessingQueue = false;
                    // Thử shop tiếp theo trong hàng đợi
                    await ProcessAudioQueueAsync();
                    return;
                }
            }

            // Đánh dấu cooldown TRƯỚC khi phát để tránh race condition
            _currentShop = nextShop;
            _shopCooldowns[nextShop.Id] = DateTime.UtcNow;

            Console.WriteLine($"[GeoFence] Entered: {_currentShop.Name} " +
                              $"(priority={_currentShop.Priority})");

            var shopToPlay = _currentShop;
            MainThread.BeginInvokeOnMainThread(() => ShopEntered?.Invoke(shopToPlay));

            bool autoPlay = Preferences.Default.Get("AutoPlayAudio", true);
            if (autoPlay)
            {
                await _audioService.PlayShopAsync(nextShop);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GeoFence] ProcessAudioQueue error: {ex.Message}");
        }
        finally
        {
            _isProcessingQueue = false;
        }
    }

    /// <summary>
    /// Callback khi Audio hoàn thành thưc thụ.
    /// Đây là cơ chế tuần tự: Khi âm thanh xong thì Auto đẩy cho các shop kế tiếp nếu nằm trồng lên nhau.
    /// </summary>
    private void OnPlaybackEnded()
    {
        if (_currentShop == null) return;

        Console.WriteLine($"[GeoFence] Audio kết thúc cho {_currentShop.Name}, kiểm tra hàng đợi tuần tự...");

        // Phát sự kiện thoát shop cũ
        var finishedShop = _currentShop;
        _currentShop = null;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            ShopExited?.Invoke(finishedShop);
        });

        // Chuyển sang shop tiếp theo trong hàng đợi ưu tiên
        _ = ProcessAudioQueueAsync();
    }

    private void CheckEnd(Location location)
    {
        if (_routeEnd == null) return;

        if (Location.CalculateDistance(location, _routeEnd, DistanceUnits.Kilometers) * 1000 < 10)
        {
            Stop();
            OnRouteFinished?.Invoke();
        }
    }

    // HELPERS
    private static double GetActivationRadius(ShopModel shop) =>
        shop.Radius > 0 ? shop.Radius : DefaultActivationRadiusM;

    private static double MetersTo(ShopModel shop, Location userLocation) =>
        Location.CalculateDistance(
            userLocation,
            new Location(shop.Latitude, shop.Longitude),
            DistanceUnits.Kilometers) * 1000;

    // ═══════ XỬ LÝ SỰ KIỆN ĐỔI NGÔN NGỮ ═══════
    // Phản hồi tức thì khi người dùng đổi ngôn ngữ trong Settings
    public async void Receive(LanguageChangedMessage message)
    {
        // ── QUAN TRỌNG: Reload toàn bộ danh sách shop từ DB theo ngôn ngữ mới ──
        // Vì GetShopsAsync() sử dụng AppLanguage preference (đã được cập nhật bởi SettingsViewModel)
        // nên tất cả AudioUrl sẽ được gán lại theo ngôn ngữ mới.
        try
        {
            var refreshedShops = await _dbService.GetShopsAsync();
            _shops = refreshedShops.ToList();
            Console.WriteLine($"[GeoFence] Đã reload {_shops.Count} shop theo ngôn ngữ: {message.Value}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GeoFence] Reload shops lỗi: {ex.Message}");
        }

        // Nếu đang phát Audio cho quán hiện tại → đổi sang audio ngôn ngữ mới
        if (_currentShop != null && _audioService.IsPlaying)
        {
            // Tạm giữ mốc thời gian (Seek Position) của Audio cũ
            double currentPos = _audioService.CurrentPosition;
            _audioService.Stop();

            // Load lại đúng shop hiện tại kèm theo ngôn ngữ mới từ DB
            var shopWithNewLang = await _dbService.GetShopAsync(_currentShop.Id);
            if (shopWithNewLang == null) return;

            // Cập nhật reference _currentShop sang bản mới
            _currentShop = shopWithNewLang;

            // Kiểm tra Offline-awareness
            bool isOfflineMode = Preferences.Default.Get("IsOfflineMode", false);
            if (isOfflineMode && !string.IsNullOrEmpty(shopWithNewLang.AudioUrl) && shopWithNewLang.AudioUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
                    {
                        await Application.Current.Windows[0].Page!.DisplayAlert(
                            "Dữ liệu chưa tải",
                            "Ngôn ngữ mới chưa được cài đặt cho chế độ Offline. Vui lòng kết nối mạng và tải lại ở mục Dữ Liệu.",
                            "OK");
                    }
                });
                return; // Ngừng phát nếu không có mạng / chưa tải
            }

            // Play URL ngôn ngữ mới tại vị trí cũ
            await _audioService.PlayShopAsync(shopWithNewLang);
            _audioService.Seek(currentPos);
        }
    }



    // Gỡ bỏ đăng ký để tránh memory leak khi class hủy
    public void Dispose()
    {
        // Gỡ bỏ đăng ký event để tránh memory leak
        _audioService.PlaybackEnded -= OnPlaybackEnded;
        // Gỡ bỏ đăng ký messenger
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    // ═══════ XỬ LÝ SỰ KIỆN FILE AUDIO MỚI ═══════
    // Khi file audio mới được ghi xuống disk:
    // Reload _shops để geofencing dùng đúng AudioUrl mới.
    // KHÔNG cần stop/play ở đây — AudioPlayerService.Receive() đã tự xử lý.
    public async void Receive(AudioFilesUpdatedMessage message)
    {
        try
        {
            var refreshedShops = await _dbService.GetShopsAsync();
            _shops = refreshedShops.ToList();
            Console.WriteLine($"[GeoFence] AudioFilesUpdated: reload {_shops.Count} shop, AudioUrl mới sẵn sàng.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GeoFence] AudioFilesUpdated reload lỗi: {ex.Message}");
        }
    }
}
