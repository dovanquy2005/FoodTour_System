using Microsoft.Maui.Devices.Sensors;
using FoodTour.Mobile.Models;
using System.Collections.Concurrent;

namespace FoodTour.Mobile.Services;

public class WalkingSimulationService
{
    private readonly DatabaseService _dbService;
    private readonly IAudioPlayerService _audioService;
    private List<ShopModel> _shops = new();
    private ShopModel? _currentShop = null; // track shop đang active
    private bool _isRunning = false;
    public Action<Location>? OnLocationUpdate;
    public Action? OnRouteFinished;

    private Location? _routeEnd;

    // Chống spam: Debounce + Cooldown
    private DateTime _lastCheckTime = DateTime.MinValue;
    private const int DebounceMs = 2_000; // ms tối thiểu giữa 2 lần check liên tiếp
    private readonly ConcurrentDictionary<string, DateTime> _shopCooldowns = new();
    private const int CooldownMinutes = 5;
    private const double DefaultActivationRadiusM = 60.0;
    private const double HysteresisMultiplier = 1.3;

    public WalkingSimulationService(DatabaseService dbService, IAudioPlayerService audioService)
    {
        _dbService = dbService;
        _audioService = audioService;
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
        _currentShop = null;

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

        OnLocationUpdate?.Invoke(e.Location);

        // Fire-and-forget: keep the handler non-blocking.
        _ = CheckShopAsync(e.Location);

        CheckEnd(e.Location);
    }
    /*
        private async Task CheckShop(Location location)
        {
            // Case 1: Đang ở trong 1 shop → kiểm tra xem còn trong radius không
            if (_currentShop != null)
            {
                double distToCurrent = Location.CalculateDistance(
                    location,
                    new Location(_currentShop.Latitude, _currentShop.Longitude),
                    DistanceUnits.Kilometers) * 1000;

                if (distToCurrent <= 50)
                    return; // Vẫn trong shop cũ → không làm gì, bám ở đây

                // Ra khỏi shop cũ → reset
                _currentShop = null;
                _audioService.Stop();
                // Không return ở đây để hỗ trợ trường hợp nhảy cóc (teleport) sang quán khác ngay lập tức
            }

            // Case 2: Tìm shop gần nhất trong 100m
            ShopModel? nearest = null;
            double minDist = double.MaxValue;

            foreach (var shop in _shops)
            {
                double dist = Location.CalculateDistance(
                    location,
                    new Location(shop.Latitude, shop.Longitude),
                    DistanceUnits.Kilometers) * 1000;

                if (dist <= 100 && dist < minDist)
                {
                    minDist = dist;
                    nearest = shop;
                }
            }

            if (nearest != null)
            {
                _currentShop = nearest;
                bool autoPlay = Microsoft.Maui.Storage.Preferences.Default.Get("AutoPlayAudio", true);
                if (autoPlay)
                {
                    await _audioService.PlayShopAsync(nearest);
                }
            }
        }
    */
    private async Task CheckShopAsync(Location userLocation)
    {
        // ── 1. DEBOUNCE ───────────────────────────────────────────────────────
        var now = DateTime.UtcNow;
        if ((now - _lastCheckTime).TotalMilliseconds < DebounceMs) return;
        _lastCheckTime = now;

        // ── 2. HYSTERESIS EXIT CHECK ──────────────────────────────────────────
        if (_currentShop is not null)
        {
            double entryRadius = GetActivationRadius(_currentShop);
            double exitRadius = entryRadius * HysteresisMultiplier;
            double distToCurrent = MetersTo(_currentShop, userLocation);

            if (distToCurrent <= exitRadius)
                return; // Still inside extended exit zone — do nothing.

            // User has truly left the shop.
            Console.WriteLine($"[GeoFence] Exited: {_currentShop.Name}");
            _currentShop = null;
            _audioService.Stop();

            // Fall through: maybe another shop is immediately nearby.
        }

        // ── 3. PRIORITY-BASED CANDIDATE SELECTION ────────────────────────────
        var candidates = _shops
            .Select(s => (shop: s, dist: MetersTo(s, userLocation), radius: GetActivationRadius(s)))
            .Where(x => x.dist <= x.radius)               // within activation zone
            .OrderByDescending(x => x.shop.Priority)       // highest priority first
            .ThenBy(x => x.dist)                           // nearest as tiebreaker
            .ToList();

        foreach (var candidate in candidates)
        {
            // ── 4. COOLDOWN CHECK ─────────────────────────────────────────────
            if (_shopCooldowns.TryGetValue(candidate.shop.Id, out var lastPlayed)
                && (now - lastPlayed).TotalMinutes < CooldownMinutes)
            {
                Console.WriteLine($"[GeoFence] Cooldown active: {candidate.shop.Name} " +
                                  $"({(int)(now - lastPlayed).TotalSeconds}s / {CooldownMinutes * 60}s)");
                continue; // Skip — try the next lower-priority candidate.
            }

            // ── 5. WINNER — stamp cooldown BEFORE async play to prevent race ──
            _currentShop = candidate.shop;
            _shopCooldowns[candidate.shop.Id] = now;

            Console.WriteLine($"[GeoFence] Entered: {_currentShop.Name} " +
                              $"(priority={_currentShop.Priority}, dist={candidate.dist:F0}m)");

            bool autoPlay = Preferences.Default.Get("AutoPlayAudio", true);
            if (autoPlay)
            {
                await _audioService.PlayShopAsync(_currentShop);
            }

            break; // Only trigger one shop per cycle.
        }
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
}