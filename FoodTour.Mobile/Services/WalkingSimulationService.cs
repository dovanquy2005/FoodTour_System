using Microsoft.Maui.Devices.Sensors;
using FoodTour.Mobile.Models;
using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.Messaging;
using FoodTour.Mobile.Messages;

namespace FoodTour.Mobile.Services;

public class WalkingSimulationService : IRecipient<LanguageChangedMessage>, IDisposable
{
    private readonly DatabaseService _dbService;
    private readonly IAudioPlayerService _audioService;
    private List<ShopModel> _shops = new();
    private ShopModel? _currentShop = null; // track shop đang active
    private bool _isRunning = false;
    public Action<Location>? OnLocationUpdate;
    public Action? OnRouteFinished;
    public event Action<ShopModel>? ShopEntered;
    public event Action<ShopModel>? ShopExited;

    private Location? _routeEnd;
    private DateTime _startTime;
    private HashSet<string> _activeZoneIds = new();
    private readonly List<ShopModel> _audioQueue = new();
    private readonly SemaphoreSlim _queueLock = new SemaphoreSlim(1, 1);

    // Chống spam: Debounce + Cooldown
    private DateTime _lastCheckTime = DateTime.MinValue;
    private const int DebounceMs = 2_000; // ms tối thiểu giữa 2 lần check liên tiếp
    private readonly ConcurrentDictionary<string, DateTime> _shopCooldowns = new();
    private const int CooldownMinutes = 1;
    private const double DefaultActivationRadiusM = 50.0;
    private const double HysteresisMultiplier = 1.3;

    public WalkingSimulationService(DatabaseService dbService, IAudioPlayerService audioService)
    {
        _dbService = dbService;
        _audioService = audioService;

        _audioService.StateChanged += OnAudioStateChanged;
        _audioService.PlaybackEnded += OnPlaybackEnded;

        // Đăng ký nhận sự kiện đổi ngôn ngữ toàn cục
        WeakReferenceMessenger.Default.Register(this);
    }

    public void SetRouteEnd(Location end) => _routeEnd = end;

    public async Task Start()
    {
        if (_isRunning) return;

        try
        {
            // Reload shops every time we start so Priority/Radius changes are picked up.
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

        OnLocationUpdate?.Invoke(e.Location);

        _ = CheckShopAsync(e.Location); // fire-and-forget, non-blocking
        CheckEnd(e.Location);
    }

    private async Task CheckShopAsync(Location userLocation)
    {
        // ── 1. DEBOUNCE ───────────────────────────────────────────────────────
        var now = DateTime.UtcNow;
        if ((now - _lastCheckTime).TotalMilliseconds < DebounceMs) return;
        _lastCheckTime = now;

        await _queueLock.WaitAsync();
        try
        {
            // ── 2. UPDATE ACTIVE ZONES ────────────────────────────────────────
            var inRadius = _shops
                .Where(s => MetersTo(s, userLocation) <= GetActivationRadius(s))
                .ToList();

            _activeZoneIds = new HashSet<string>(inRadius.Select(s => s.Id));

            // ── 3. HYSTERESIS: protect currently playing shop ─────────────────
            if (_currentShop != null)
            {
                double exitRadius = GetActivationRadius(_currentShop) * HysteresisMultiplier;
                double distToCurrent = MetersTo(_currentShop, userLocation);

                if (distToCurrent <= exitRadius)
                    return; // Still inside extended exit zone — do nothing.

                Console.WriteLine($"[GeoFence] Exited: {_currentShop.Name}");
                var exitedShop = _currentShop;
                _currentShop = null;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ShopExited?.Invoke(exitedShop);
                });

                _audioService.Stop();
                // Fall through: maybe another shop is immediately nearby.
            }

            // ── 4. REMOVE QUEUED SHOPS WHOSE ZONE THE USER HAS LEFT ──────────
            int removedFromQueue = _audioQueue.RemoveAll(q => !_activeZoneIds.Contains(q.Id));
            if (removedFromQueue > 0)
                Console.WriteLine($"[GeoFence] Pruned {removedFromQueue} shop(s) from queue (zone exited).");

            // ── 5. FIND NEW CANDIDATES TO ENQUEUE ────────────────────────────
            var alreadyScheduledIds = new HashSet<string>(_audioQueue.Select(q => q.Id));
            if (_currentShop != null) alreadyScheduledIds.Add(_currentShop.Id);

            var newCandidates = inRadius
                .Where(s => !alreadyScheduledIds.Contains(s.Id) && !IsInCooldown(s, now))
                .OrderByDescending(s => s.Priority)
                .ThenBy(s => MetersTo(s, userLocation))
                .ToList();

            // ── 6. ENQUEUE — maintaining descending priority order ─────────────
            foreach (var candidate in newCandidates)
            {
                int insertAt = _audioQueue.FindIndex(q => q.Priority < candidate.Priority);
                if (insertAt < 0)
                    _audioQueue.Add(candidate);
                else
                    _audioQueue.Insert(insertAt, candidate);

                Console.WriteLine($"[GeoFence] Enqueued: {candidate.Name} (priority={candidate.Priority})");
            }

            // ── 7. START PLAYING IF IDLE ──────────────────────────────────────
            if (_currentShop == null && _audioQueue.Count > 0)
            {
                await AdvanceQueueLocked(now);
            }
        }
        finally
        {
            _queueLock.Release();
        }
    }

    private async Task AdvanceQueueAsync()
    {
        await _queueLock.WaitAsync();
        try
        {
            var finishedShop = _currentShop;
            _currentShop = null;

            if (finishedShop != null)
            {
                MainThread.BeginInvokeOnMainThread(() => ShopExited?.Invoke(finishedShop));
            }

            await AdvanceQueueLocked(DateTime.UtcNow);
        }
        finally
        {
            _queueLock.Release();
        }
    }

    private async Task AdvanceQueueLocked(DateTime now)
    {
        while (_audioQueue.Count > 0)
        {
            var next = _audioQueue[0];
            _audioQueue.RemoveAt(0);

            // Skip if user has left this shop's zone while it was waiting.
            if (!_activeZoneIds.Contains(next.Id))
            {
                Console.WriteLine($"[Queue] Skip {next.Name} — user no longer in zone.");
                continue;
            }

            // Re-check cooldown at dequeue time
            if (IsInCooldown(next, now))
            {
                Console.WriteLine($"[Queue] Skip {next.Name} — cooldown active.");
                continue;
            }

            // ── Winner ──────────────────────────────────────────────────────
            _currentShop = next;
            // Stamp cooldown BEFORE awaiting PlayShopAsync to prevent re-entry
            _shopCooldowns[next.Id] = now;

            Console.WriteLine($"[Queue] Playing: {next.Name} (priority={next.Priority})");

            var shopToPlay = _currentShop;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ShopEntered?.Invoke(shopToPlay);
            });

            bool autoPlay = Preferences.Default.Get("AutoPlayAudio", true);
            if (autoPlay)
            {
                await _audioService.PlayShopAsync(next);
            }

            return; // done for this cycle; next track starts via PlaybackEnded
        }

        // Queue exhausted.
        _currentShop = null;
        Console.WriteLine("[Queue] Queue empty — all done.");
    }

    private async Task ClearQueueAsync()
    {
        await _queueLock.WaitAsync();
        try
        {
            if (_audioService.IsPlaying || _audioService.IsPlayerVisible)
            {
                Console.WriteLine("[Queue] ClearQueue skipped — new shop already active (internal transition stop).");
                return;
            }

            var stoppedShop = _currentShop;
            _currentShop = null;

            // Reset circle cho shop bị user dừng thủ công
            if (stoppedShop != null)
                MainThread.BeginInvokeOnMainThread(() => ShopExited?.Invoke(stoppedShop));

            if (_audioQueue.Count > 0)
            {
                Console.WriteLine($"[Queue] Cleared {_audioQueue.Count} item(s) — player stopped by user.");
                _audioQueue.Clear();
            }
        }
        finally
        {
            _queueLock.Release();
        }
    }

    private void OnAudioStateChanged()
    {
        // Detect explicit user stop: player becomes invisible (not just paused) -> clear the queue
        if (!_audioService.IsPlayerVisible && !_audioService.IsPlaying)
        {
            _ = ClearQueueAsync();
        }
    }

    private void OnPlaybackEnded()
    {
        _ = AdvanceQueueAsync();
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

    // HELPERS
    private static double GetActivationRadius(ShopModel shop) =>
        shop.Radius > 0 ? shop.Radius : DefaultActivationRadiusM;

    private static double MetersTo(ShopModel shop, Location userLocation) =>
        Location.CalculateDistance(
            userLocation,
            new Location(shop.Latitude, shop.Longitude),
            DistanceUnits.Kilometers) * 1000;
    private bool IsInCooldown(ShopModel shop, DateTime now) =>
        _shopCooldowns.TryGetValue(shop.Id, out var lastPlayed)
        && (now - lastPlayed).TotalMinutes < CooldownMinutes;

    // Gỡ bỏ đăng ký để tránh memory leak khi class hủy
    public void Dispose()
    {
        _audioService.StateChanged -= OnAudioStateChanged;
        _audioService.PlaybackEnded -= OnPlaybackEnded;

        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}