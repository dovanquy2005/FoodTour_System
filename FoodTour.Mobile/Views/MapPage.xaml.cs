using FoodTour.Mobile.Services;
using FoodTour.Mobile.ViewModels;
using FoodTour.Mobile.Messages;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using System.Collections.Specialized;

namespace FoodTour.Mobile.Views;

public partial class MapPage : ContentPage
{
    private MapViewModel _viewModel;
    private string _lastEnteredShopName = string.Empty; // Lưu tên quán vừa ghé để tránh lặp lại thuyết minh
    private Location _userLocation = new Location(10.761884, 106.702000); // Vị trí người dùng
    private Models.ShopModel? _pendingRouteShop = null; // Quán cần chỉ đường nếu Map chưa vẽ xong

    public MapPage(MapViewModel vm)
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MapPage InitializeComponent error: {ex.Message}");
            // Hiển thị thông báo thay vì crash ứng dụng nếu lỗi Google Play Services
            Content = new Grid
            {
                Children = {
                    new Label {
                        Text = "Không thể tải bản đồ. Vui lòng kiểm tra Google Play Services.",
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        FontSize = 16
                    }
                }
            };
            BindingContext = vm;
            _viewModel = vm;
            return; // Thoát sớm — MainMap là null khi InitializeComponent thất bại
        }
        BindingContext = vm;
        _viewModel = vm;
        MainMap.Loaded += MainMap_Loaded;

        // Xử lý sự kiện "Dẫn Đường"
        WeakReferenceMessenger.Default.Register<RouteToShopMessage>(this, (r, m) =>
        {
            if (MainMap != null && MainMap.Handler != null)
            {
                // Map đã sẵn sàng, vẽ đường ngay lập tức
                DrawRouteToShop(m.TargetShop);
            }
            else
            {
                // Map chưa khởi tạo xong, lưu lại và vẽ khi Loaded
                _pendingRouteShop = m.TargetShop;
            }
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Chúng ta không thực hiện vẽ map hay MoveToRegion ở đây nữa, 
            // vì Google Map View bên dưới Android có thể chưa khởi tạo xong và gây lỗi NullReferenceException (Văng app).
            // Logic liên quan đến UI của bản đồ đã được chuyển sang MainMap_Loaded

            // Khởi động hệ thống GPS
            await SetupGps();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MapPage OnAppearing error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        try
        {
            // Đã hủy đăng ký sự kiện CollectionChanged
            Geolocation.Default.LocationChanged -= OnUserLocationChanged;

            // QUAN TRỌNG: Dừng hẳn việc lắng nghe GPS khi người dùng thoát tab Map để tiết kiệm PIN
            if (Geolocation.Default.IsListeningForeground)
            {
                Geolocation.Default.StopListeningForeground();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MapPage OnDisappearing error: {ex.Message}");
        }
    }

    private async void MainMap_Loaded(object? sender, EventArgs e)
    {
        try
        {
            // Chờ handler sẵn sàng một chút
            if (MainMap != null && MainMap.Handler == null)
            {
                await Task.Delay(200);
            }

            // Load dữ liệu từ Database nếu danh sách đang trống
            if (_viewModel.Shops == null || _viewModel.Shops.Count == 0)
            {
                await _viewModel.LoadData();
            }

            // Vẽ vòng bán kính xung quanh các quán ăn
            DrawRadiusCircles();

            if (MainMap != null)
            {
                MainMap.IsShowingUser = true;
            }

            // Nếu có yêu cầu chỉ đường từ trang Khám Phá, vẽ đường ngay
            if (_pendingRouteShop != null)
            {
                DrawRouteToShop(_pendingRouteShop);
                _pendingRouteShop = null; // Reset sau khi xử lý xong
            }
            else
            {
                // Chỉ tập trung vào vị trí người dùng nếu KHÔNG có yêu cầu chỉ đường
                MainMap?.MoveToRegion(MapSpan.FromCenterAndRadius(_userLocation, Distance.FromMeters(250)));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MainMap_Loaded error: {ex.Message}");
        }
    }

    private void DrawRadiusCircles()
    {
        if (MainMap == null) return;

        MainMap.MapElements.Clear();
        MainMap.Pins.Clear();

        if (_viewModel.Shops == null || _viewModel.Shops.Count == 0) return;

        foreach (var shop in _viewModel.Shops)
        {
            try
            {
                // Vẽ vòng tròn bán kính 100m
                var circle = new Circle
                {
                    Center = shop.Location,
                    Radius = Distance.FromMeters(100), // Bán kính nhận diện 100m
                    StrokeColor = Colors.Red,
                    StrokeWidth = 2,
                    FillColor = Colors.Red.WithAlpha(0.25f)
                };
                MainMap.MapElements.Add(circle);

                // Thêm cọc (Pin) ở giữa tâm
                var pin = new Pin
                {
                    Label = shop.Name,
                    Address = shop.Address,
                    Type = PinType.Place,
                    Location = shop.Location,
                    BindingContext = shop // Gắn dữ liệu shop vào Pin để xử lý khi click
                };

                // Gắn sự kiện click vào Pin (mở trang chi tiết, không thuyết minh)
                pin.MarkerClicked += OnPinClicked;

                MainMap.Pins.Add(pin);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DrawCircle error: {ex.Message}");
            }
        }
    }

    private void DrawRouteToShop(Models.ShopModel targetShop)
    {
        if (MainMap == null) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                // Xóa mọi Polyline cũ trước khi vẽ đường mới
                var oldPolylines = MainMap.MapElements.Where(e => e is Polyline).ToList();
                foreach (var line in oldPolylines)
                {
                    MainMap.MapElements.Remove(line);
                }

                // Vẽ đường chim bay nét đứt (thực ra nét đứt phụ thuộc nền tảng, ta dùng stroke dày)
                var routeLine = new Polyline
                {
                    StrokeColor = Colors.Blue,
                    StrokeWidth = 8,
                    Geopath =
                    {
                        _userLocation,          // Điểm bắt đầu (Người dùng)
                        targetShop.Location     // Điểm kết thúc (Quán)
                    }
                };

                MainMap.MapElements.Add(routeLine);

                // Dời khung hình (Camera) ra giữa hai điểm để người dùng thấy tổng quan
                double centerLat = (_userLocation.Latitude + targetShop.Location.Latitude) / 2;
                double centerLng = (_userLocation.Longitude + targetShop.Location.Longitude) / 2;
                double distanceKm = Location.CalculateDistance(_userLocation, targetShop.Location, DistanceUnits.Kilometers);
                
                // Mở rộng bán kính hiển thị thêm 20% cho thoải mái
                double radiusMeters = (distanceKm * 1000) * 0.6; 
                if (radiusMeters < 300) radiusMeters = 300; // Tối thiểu 300m

                MainMap.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(centerLat, centerLng), Distance.FromMeters(radiusMeters)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DrawRoute error: {ex.Message}");
            }
        });
    }

    private async Task SetupGps()
    {
        try
        {
            // Kiểm tra quyền truy cập vị trí
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status == PermissionStatus.Granted)
            {
                // Đăng ký nhận thông tin vị trí mới
                Geolocation.Default.LocationChanged -= OnUserLocationChanged;
                Geolocation.Default.LocationChanged += OnUserLocationChanged;

                var request = new GeolocationListeningRequest(
                    GeolocationAccuracy.Best,
                    TimeSpan.FromSeconds(1));

                await Geolocation.Default.StartListeningForegroundAsync(request);

                var location = await Geolocation.Default.GetLocationAsync();

                if (location != null)
                {
                    _userLocation = location;

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        MainMap?.MoveToRegion(
                            MapSpan.FromCenterAndRadius(
                                location,
                                Distance.FromMeters(300)));
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi GPS: {ex.Message}");
        }
    }

    private void OnUserLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        try
        {
            var userLoc = e.Location;
            _userLocation = userLoc;
            if (userLoc == null || _viewModel.Shops == null) return;

            // Cập nhật vị trí người dùng trên map
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (MainMap == null) return;

                var position = new Location(userLoc.Latitude, userLoc.Longitude);
                MainMap.MoveToRegion(
                    MapSpan.FromCenterAndRadius(position, Distance.FromMeters(300)));
            });

            bool insideAnyShop = false;

            foreach (var shop in _viewModel.Shops)
            {
                double distanceKm = Location.CalculateDistance(userLoc, shop.Location, DistanceUnits.Kilometers);

                if (distanceKm < 0.1) // Trong bán kính 100m
                {
                    insideAnyShop = true;

                    // Chỉ kích hoạt nếu đây là quán mới (tránh lặp khi đứng yên)
                    if (_lastEnteredShopName != shop.Name)
                    {
                        _lastEnteredShopName = shop.Name;

                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            try
                            {
                                HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);

                                // 1. Đọc thông báo vào shop
                                await _viewModel.AnnounceEnterShop(shop);

                                // 2. Bắt đầu thuyết minh
                                await _viewModel.OnEnterShop(shop);
                            }
                            catch { }
                        });
                    }

                    return; // Đã tìm thấy quán gần nhất, không cần kiểm tra thêm
                }
            }

            // Người dùng đã ra khỏi tất cả các shop
            if (!insideAnyShop && _lastEnteredShopName != string.Empty)
            {
                _lastEnteredShopName = string.Empty;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _viewModel.OnExitShop();
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Location change error: {ex.Message}");
        }
    }

    private async void OnPinClicked(object? sender, PinClickedEventArgs e)
    {
        try
        {
            e.HideInfoWindow = false;
            var pin = sender as Pin;

            if (pin?.BindingContext is Models.ShopModel selectedPoi)
            {
                await _viewModel.GoToDetailCommand.ExecuteAsync(selectedPoi);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Pin click error: {ex.Message}");
        }
    }

    private void OnPoiSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (e.CurrentSelection.FirstOrDefault() is Models.ShopModel selectedPoi)
            {
                if (MainMap != null)
                    MainMap.MoveToRegion(MapSpan.FromCenterAndRadius(selectedPoi.Location, Distance.FromMeters(300)));

                // Reset Selection để có thể bấm lại vào chính quán đó sau này
                ((CollectionView)sender).SelectedItem = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Selection error: {ex.Message}");
        }
    }
}