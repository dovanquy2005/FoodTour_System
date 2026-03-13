using Microsoft.Maui.Devices.Sensors;
using FoodTour.Mobile.Models;

namespace FoodTour.Mobile.Services;

public class WalikingSimulationService
{
    private readonly List<ShopModel> _shops;
    private bool _insideShop = false;
    private bool _isRunning = false;
    public Action<Location>? OnLocationUpdate;
    public Func<ShopModel, Task>? OnEnterShop;           // Gọi để bắt đầu thuyết minh
    public Func<ShopModel, Task>? OnAnnounceEnterShop;   // Đọc thông báo "Bạn đã vào trong phạm vi quán..."
    public Action? OnExitShop;
    public Action? OnRouteFinished;

    private Location? _routeEnd;

    public WalikingSimulationService(List<ShopModel> shops)
    {
        _shops = shops;
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
        _insideShop = false;

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

    private async Task CheckShop(Location location)
    {
        foreach (var shop in _shops)
        {
            var shopLoc = new Location(shop.Latitude, shop.Longitude);

            double distance =
                Location.CalculateDistance(
                    location,
                    shopLoc,
                    DistanceUnits.Kilometers) * 1000;

            if (distance <= 100)
            {
                if (!_insideShop)
                {
                    _insideShop = true;

                    // 1. Await thông báo xong hẳn rồi mới tiếp tục
                    if (OnAnnounceEnterShop != null)
                        await OnAnnounceEnterShop(shop);

                    // 2. Await thuyết minh
                    if (OnEnterShop != null)
                        await OnEnterShop(shop);
                }

                return;
            }
        }

        // Người dùng đã ra khỏi tất cả các shop
        if (_insideShop)
        {
            _insideShop = false;
            OnExitShop?.Invoke();
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