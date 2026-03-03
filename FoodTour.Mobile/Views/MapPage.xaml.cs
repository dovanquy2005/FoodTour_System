using FoodTour.Mobile.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using System.Collections.Specialized;

namespace FoodTour.Mobile.Views;

public partial class MapPage : ContentPage
{
    private MapViewModel _viewModel;

    public MapPage(MapViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _viewModel = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var hanoiLocation = new Location(21.028511, 105.854444);
        // Đã đổi TourMap thành MainMap
        MainMap.MoveToRegion(MapSpan.FromCenterAndRadius(hanoiLocation, Distance.FromKilometers(2)));

        _viewModel.Pois.CollectionChanged += Pois_CollectionChanged;

        DrawRadiusCircles();

        await SetupGps();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.Pois.CollectionChanged -= Pois_CollectionChanged;
        // Dọn dẹp GPS khi thoát trang Map
        Geolocation.Default.LocationChanged -= OnUserLocationChanged;
    }

    private void Pois_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        DrawRadiusCircles();
    }

    private void DrawRadiusCircles()
    {
        // Đã đổi TourMap thành MainMap
        MainMap.MapElements.Clear();

        if (_viewModel.Pois == null || _viewModel.Pois.Count == 0) return;

        foreach (var shop in _viewModel.Pois)
        {
            var circle = new Circle
            {
                Center = shop.Location,
                Radius = Distance.FromMeters(100),
                StrokeColor = Colors.Red,
                StrokeWidth = 2,
                FillColor = Color.FromRgba(255, 0, 0, 60)
            };
            // Đã đổi TourMap thành MainMap
            MainMap.MapElements.Add(circle);
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
                // Đã đổi TourMap thành MainMap
                MainMap.IsShowingUser = true;
                Geolocation.Default.LocationChanged += OnUserLocationChanged;
                var request = new GeolocationListeningRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(3));
                await Geolocation.Default.StartListeningForegroundAsync(request);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi GPS: {ex.Message}");
        }
    }

    private void OnUserLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        var userLoc = e.Location;
        if (userLoc == null) return;

        foreach (var shop in _viewModel.Pois)
        {
            double distanceKm = Location.CalculateDistance(userLoc, shop.Location, DistanceUnits.Kilometers);

            if (distanceKm < 0.1)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
                    await _viewModel.OnEnterShop(shop);
                });
            }
        }
    }

    private async void OnPinClicked(object sender, PinClickedEventArgs e)
    {
        e.HideInfoWindow = false;
        var pin = sender as Pin;

        if (pin?.BindingContext is Models.PoiModel selectedPoi)
        {
            await _viewModel.GoToDetailCommand.ExecuteAsync(selectedPoi);
        }
    }

    // Đã thêm hàm này để tránh lỗi khi click vào quán ăn trong danh sách (CollectionView)
    private void OnPoiSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Models.PoiModel selectedPoi)
        {
            // Focus bản đồ vào quán ăn vừa chọn
            MainMap.MoveToRegion(MapSpan.FromCenterAndRadius(selectedPoi.Location, Distance.FromMeters(300)));

            // Xóa selection để có thể click lại lần sau
            ((CollectionView)sender).SelectedItem = null;
        }
    }
}