using Microsoft.Maui.Devices.Sensors;
using FoodTour.Mobile.Models;

namespace FoodTour.Mobile.Services;

public class WalikingSimulationService
{
    private readonly DatabaseService _dbService;
    private readonly IAudioPlayerService _audioService;
    private List<ShopModel> _shops = new();
    private ShopModel? _currentShop = null; // trachk shop đang active
    private bool _isRunning = false;
    public Action<Location>? OnLocationUpdate;
    public Action? OnRouteFinished;

    private Location? _routeEnd;

    public WalikingSimulationService(DatabaseService dbService, IAudioPlayerService audioService)
    {
        _dbService = dbService;
        _audioService = audioService;
    }

    public void SetRouteEnd(Location end)
    {
        _routeEnd = end;
    }

    public async Task Start()
    {
        if (_isRunning) return;

        try
        {
            if (_shops.Count == 0)
            {
                var data = await _dbService.GetShopsAsync();
                _shops = data.ToList();
            }
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted) return;

            // Hủy listener cũ trước khi đăng ký mới
            Geolocation.Default.LocationChanged -= OnLocationChanged;
            Geolocation.Default.LocationChanged += OnLocationChanged;

            if (!Geolocation.Default.IsListeningForeground)
            {
                var request = new GeolocationListeningRequest(
                    GeolocationAccuracy.Best,
                    TimeSpan.FromSeconds(1)); // Update mỗi 1 giây

                await Geolocation.Default.StartListeningForegroundAsync(request);
            }

            _isRunning = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WalkingSimulationService Start error: {ex.Message}");
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
            Console.WriteLine($"WalkingSimulationService Stop error: {ex.Message}");
        }
    }

    private void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        if (!_isRunning) return;

        var location = e.Location;
        if (location == null) return;

        OnLocationUpdate?.Invoke(location);

        _ = CheckShop(location);
        CheckEnd(location);
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
private async Task CheckShop(Location location)
    {
        if (_currentShop != null)
        {
            double distToCurrent = Location.CalculateDistance(
                location,
                new Location(_currentShop.Latitude, _currentShop.Longitude),
                DistanceUnits.Kilometers) * 1000;

            // 1. TĂNG BÁN KÍNH THOÁT LÊN 70m
            // Tạo vùng đệm rộng hơn để khi khách đang ngồi ăn (máy bị nhiễu GPS) không bị tắt tiếng giữa chừng.
            if (distToCurrent <= 70)
                return; 

            _currentShop = null;
            _audioService.Stop();
        }

        ShopModel? nearest = null;
        double minDist = double.MaxValue;

        foreach (var shop in _shops)
        {
            double dist = Location.CalculateDistance(
                location,
                new Location(shop.Latitude, shop.Longitude),
                DistanceUnits.Kilometers) * 1000;

            // 2. TĂNG BÁN KÍNH VÀO LÊN 60m (BÁN KÍNH ĐÓN ĐẦU)
            // Bù trừ độ trễ GPS. Khi GPS báo 60m, khách thực tế có thể đang ở mốc 50-55m. 
            // App sẽ phát âm thanh ngay lúc này, mang lại cảm giác "vừa chạm vạch là kêu".
            if (dist <= 60 && dist < minDist)
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

    private void CheckEnd(Location location)
    {
        if (_routeEnd == null)
            return;

        double distance =
            Location.CalculateDistance(
                location,
                _routeEnd,
                DistanceUnits.Kilometers) * 1000;

        if (distance < 10)
        {
            Stop();
            OnRouteFinished?.Invoke();
        }
    }
}