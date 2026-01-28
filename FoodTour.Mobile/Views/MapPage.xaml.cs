using FoodTour.Mobile.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;

namespace FoodTour.Mobile.Views;

public partial class MapPage : ContentPage
{
    private MapViewModel _viewModel;

    public MapPage(MapViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _viewModel = vm; // Lưu lại để gọi hàm
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // 1. Ép bản đồ về Hồ Gươm (Hà Nội)
        var hanoiLocation = new Location(21.028511, 105.854444);
        TourMap.MoveToRegion(MapSpan.FromCenterAndRadius(hanoiLocation, Distance.FromKilometers(2)));

        DrawRadiusCircles();
        await SetupGps();
    }

    private void DrawRadiusCircles()
    {
        TourMap.MapElements.Clear();
        foreach (var shop in _viewModel.Pois)
        {
            var circle = new Circle
            {
                Center = shop.Location,
                Radius = Distance.FromMeters(100), // Bán kính 100m
                StrokeColor = Colors.Red,
                StrokeWidth = 2,
                FillColor = Color.FromRgba(255, 0, 0, 60)
            };
            TourMap.MapElements.Add(circle);
        }
    }

    private async Task SetupGps()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status == PermissionStatus.Granted)
            {
                TourMap.IsShowingUser = true;

                // Đăng ký sự kiện
                Geolocation.Default.LocationChanged += OnUserLocationChanged;

                var request = new GeolocationListeningRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(3));

                // Dùng hàm chuẩn .NET MAUI mới
                await Geolocation.Default.StartListeningForegroundAsync(request);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi GPS: {ex.Message}");
        }
    }

    // LOGIC CHÍNH: Phát hiện vào vùng đỏ -> Gọi ViewModel bật Player
    private void OnUserLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        var userLoc = e.Location;
        if (userLoc == null) return;

        foreach (var shop in _viewModel.Pois)
        {
            double distanceKm = Location.CalculateDistance(userLoc, shop.Location, DistanceUnits.Kilometers);

            // Vào vùng 100m
            if (distanceKm < 0.1)
            {
                // Gọi ViewModel để hiện Player và đọc
                // Dùng MainThread để vẽ UI an toàn
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    // Rung 1 cái báo hiệu
                    HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);

                    // Kích hoạt Player trong ViewModel
                    await _viewModel.OnEnterShop(shop);
                });
            }
        }
    }
}