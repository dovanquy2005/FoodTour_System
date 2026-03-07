using FoodTour.Mobile.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using System.Collections.Specialized;

namespace FoodTour.Mobile.Views;

public partial class MapPage : ContentPage
{
    private MapViewModel _viewModel;
    private string _lastEnteredShopName = string.Empty; // Lưu tên quán vừa ghé để tránh lặp lại thuyết minh
    private Location _userLocation = new Location(10.762636086391392, 106.70256329960525); // Vị trí người dùng
    private bool _firstGpsFix = false;

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

            // Tọa độ trung tâm đường Vĩnh Khánh (khoảng Ốc Oanh)
            var location = _userLocation;
            MainMap.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(1.0)));
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

                // Gắn sự kiện click vào Pin
                pin.MarkerClicked += OnPinClicked;

                MainMap.Pins.Add(pin);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DrawCircle error: {ex.Message}");
            }
        }
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
#if DEBUG
                Geolocation.Default.StopListeningForeground();

                var fakeLocation = new Location(10.762636086391392, 106.70256329960525);

                _userLocation = fakeLocation;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (MainMap != null)
                    {
                        MainMap.MoveToRegion(
                            MapSpan.FromCenterAndRadius(
                                fakeLocation,
                                Distance.FromMeters(300)));
                    }
                });
#endif

                // Hiển thị chấm xanh vị trí người dùng (Đảm bảo chạy trên luồng chính)
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (MainMap != null) MainMap.IsShowingUser = true;
                });

                // Đăng ký nhận thông tin vị trí mới
                Geolocation.Default.LocationChanged -= OnUserLocationChanged;
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
        try
        {
            var userLoc = e.Location;
            _userLocation = userLoc;
            if (userLoc == null || _viewModel.Shops == null) return;

            // Hiển thị vị trí người dùng trên map
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (MainMap == null) return;

                var position = new Location(userLoc.Latitude, userLoc.Longitude);

                // if (!_firstGpsFix)
                // {
                //     MainMap.MoveToRegion(
                //         MapSpan.FromCenterAndRadius(
                //             position,
                //             Distance.FromMeters(300)));

                //     _firstGpsFix = true;
                // }

                MainMap.MoveToRegion(
                    MapSpan.FromCenterAndRadius(position, Distance.FromMeters(300)));
            });

            foreach (var shop in _viewModel.Shops)
            {
                // Tính khoảng cách từ người dùng đến quán
                double distanceKm = Location.CalculateDistance(userLoc, shop.Location, DistanceUnits.Kilometers);

                // Nếu người dùng vào trong bán kính 100m (0.1km)
                if (distanceKm < 0.1)
                {
                    // KIỂM TRA: Chỉ gọi thuyết minh nếu đây là quán mới (tránh đọc lặp lại khi đang đứng yên)
                    if (_lastEnteredShopName != shop.Name)
                    {
                        _lastEnteredShopName = shop.Name;

                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            try
                            {
                                HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
                                await _viewModel.OnEnterShop(shop);
                            }
                            catch { }
                        });
                    }
                    return; // Đã tìm thấy quán gần nhất, không cần kiểm tra các quán khác nữa
                }
            }

            // Nếu người dùng không còn ở gần quán nào thì reset
            _lastEnteredShopName = string.Empty;
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