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
    private Location? _userLocation = null; // Vị trí thật của người dùng
    private Models.ShopModel? _pendingRouteShop = null; // Quán cần chỉ đường nếu Map chưa vẽ xong

    // Follow state machine
    private enum FollowState { Following, Free, RouteView }
    private FollowState _followState = FollowState.Following;

    private const double FollowRadiusMeters = 250;

    // Throttle MoveToRegion
    private DateTime _lastMoveTime = DateTime.MinValue;
    private const int MoveThrottleMs = 800;
    // Debounce timer: sau 5s không kéo -> tự resume follow
    private CancellationTokenSource _resumeCts = new();
    private const int ResumeDelayMs = 30_000;
    private bool _isProgrammaticMove = false; // Flag phân biệt MoveToRegion với gesture của user
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
            _resumeCts.Cancel();
            // Đã hủy đăng ký sự kiện CollectionChanged
            Geolocation.Default.LocationChanged -= OnUserLocationChanged;
#if ANDROID
            UnhookAndroid();
#endif
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

    // MAP LOADED
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
                MainMap.IsShowingUser = true;

            // Hook gesture dectector của native map platform
#if ANDROID
            HookAndroid();
#endif

            // Nếu có yêu cầu chỉ đường từ trang Khám Phá, vẽ đường ngay
            if (_pendingRouteShop != null)
            {
                DrawRouteToShop(_pendingRouteShop);
                _pendingRouteShop = null; // Reset sau khi xử lý xong
            }
            else if (_userLocation != null)
            {
                // Chỉ tập trung vào vị trí người dùng nếu KHÔNG có yêu cầu chỉ đường
                TransitionTo(FollowState.Following);
                MoveCamera(_userLocation, FollowRadiusMeters);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MainMap_Loaded error: {ex.Message}");
        }
    }

    /// <summary>
    /// NATIVE GESTURE HOOK. Ta hook xuống GoogleMap native để detect, sau đó gọi OnUserDraggedMap().
    /// Do MAUI Maps abstraction không expose "user dragged map" event.
    /// </summary>
#if ANDROID
    private Android.Gms.Maps.GoogleMap? _googleMap;

    private void HookAndroid()
    {
        try
        {
            if (MainMap?.Handler?.PlatformView is Android.Gms.Maps.MapView mapView)
            {
                mapView.GetMapAsync(new MapReadyCallback(gmap =>
                {
                    _googleMap = gmap;
                    // CameraMove fires bất cứ khi nào camera bắt đầu di chuyển
                    // (cả code lẫn gesture) — ta dùng _isProgrammaticMove để lọc
                    _googleMap.CameraMove += OnAndroidCameraMove;
                }));
            }
        }
        catch (Exception ex) { Console.WriteLine($"HookAndroid error: {ex.Message}"); }
    }

    private void UnhookAndroid()
    {
        if (_googleMap != null)
        {
            _googleMap.CameraMove -= OnAndroidCameraMove;
            _googleMap = null;
        }
    }

    private void OnAndroidCameraMove(object? sender, EventArgs e)
    {
        if (!_isProgrammaticMove)
            OnUserDraggedMap();
    }

    private class MapReadyCallback : Java.Lang.Object, Android.Gms.Maps.IOnMapReadyCallback
    {
        private readonly Action<Android.Gms.Maps.GoogleMap> _cb;
        public MapReadyCallback(Action<Android.Gms.Maps.GoogleMap> cb) => _cb = cb;
        public void OnMapReady(Android.Gms.Maps.GoogleMap googleMap) => _cb(googleMap);
    }
#endif

    // USER DRAGGED MAP
    private void OnUserDraggedMap()
    {
        if (_followState == FollowState.Free) return; // Đã ở Free rồi, reset timer thôi

        TransitionTo(FollowState.Free);
        ScheduleResumeFollow();
    }

    // STATE MACHINE
    private void TransitionTo(FollowState newState) => _followState = newState;

    // AUTO-RESUME
    private void ScheduleResumeFollow()
    {
        // Reset đồng hồ mỗi lần user tương tác thêm
        _resumeCts.Cancel();
        _resumeCts = new CancellationTokenSource();
        var token = _resumeCts.Token;

        Task.Delay(ResumeDelayMs, token).ContinueWith(t =>
        {
            if (t.IsCanceled) return;

            TransitionTo(FollowState.Following);
            if (_userLocation == null) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (MainMap?.Handler != null)
                    MoveCamera(_userLocation, FollowRadiusMeters);
            });
        }, TaskScheduler.Default);
    }

    // GPS SETUP
    private async Task SetupGps()
    {
        try
        {
            // Kiểm tra quyền truy cập vị trí
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted) return;

            // Đăng ký nhận thông tin vị trí mới
            Geolocation.Default.LocationChanged -= OnUserLocationChanged;
            Geolocation.Default.LocationChanged += OnUserLocationChanged;

            await Geolocation.Default.StartListeningForegroundAsync(
                new GeolocationListeningRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(1))
            );

            var location = await Geolocation.Default.GetLocationAsync();

            if (location != null)
            {
                _userLocation = location;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TransitionTo(FollowState.Following);
                    if (MainMap?.Handler != null)
                    {
                        MoveCamera(location, FollowRadiusMeters);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi GPS: {ex.Message}");
        }
    }

    //LOCATION CHANGED - CORE FOLLOW LOOP
    private void OnUserLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        try
        {
            var userLoc = e.Location;
            if (userLoc == null) return;

            _userLocation = userLoc;

            if (_followState == FollowState.Following)
            {
                var now = DateTime.UtcNow;
                if ((now - _lastMoveTime).TotalMilliseconds >= MoveThrottleMs)
                {
                    _lastMoveTime = now;
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (MainMap?.Handler != null)
                            MoveCamera(userLoc, FollowRadiusMeters);
                    });
                }
            }

            if (_viewModel.Shops == null) return;

            foreach (var shop in _viewModel.Shops)
            {
                if (Location.CalculateDistance(userLoc, shop.Location, DistanceUnits.Kilometers) < 0.1)
                {
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
                    return; // Đã tìm thấy quán gần nhất, không cần kiểm tra thêm
                }
            }

            // Người dùng đã ra khỏi tất cả các shop
            if (_lastEnteredShopName != string.Empty)
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

    // DRAW ROUTE
    private void DrawRouteToShop(Models.ShopModel targetShop)
    {
        if (MainMap == null) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                // Không thể vẽ đường nếu chưa biết vị trí người dùng
                if (_userLocation == null) return;

                TransitionTo(FollowState.RouteView);
                ScheduleResumeFollow();

                var oldPolylines = MainMap.MapElements.Where(e => e is Polyline).ToList();
                foreach (var line in oldPolylines)
                    MainMap.MapElements.Remove(line);

                MainMap.MapElements.Add(new Polyline
                {
                    StrokeColor = Colors.Blue,
                    StrokeWidth = 8,
                    Geopath = { _userLocation, targetShop.Location }
                });

                double centerLat = (_userLocation.Latitude + targetShop.Location.Latitude) / 2;
                double centerLng = (_userLocation.Longitude + targetShop.Location.Longitude) / 2;
                double distanceKm = Location.CalculateDistance(_userLocation, targetShop.Location, DistanceUnits.Kilometers);
                double radiusMeters = Math.Max((distanceKm * 1000) * 0.6, 300);

                MoveCamera(new Location(centerLat, centerLng), radiusMeters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DrawRoute error: {ex.Message}");
            }
        });
    }

    // HELPERS
    private void MoveCamera(Location location, double radiusMeters)
    {
        _isProgrammaticMove = true;
        MainMap?.MoveToRegion(
            MapSpan.FromCenterAndRadius(location, Distance.FromMeters(radiusMeters)));
        _isProgrammaticMove = false;
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
                // Vẽ vòng tròn bán kính 100m (dùng Inline Object Intialization)
                MainMap.MapElements.Add(new Circle
                {
                    Center = shop.Location,
                    Radius = Distance.FromMeters(100), // Bán kính nhận diện 100m
                    StrokeColor = Colors.Red,
                    StrokeWidth = 2,
                    FillColor = Colors.Red.WithAlpha(0.25f)
                });

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

    private async void OnPinClicked(object? sender, PinClickedEventArgs e)
    {
        try
        {
            e.HideInfoWindow = false;
            var pin = sender as Pin;

            if ((sender as Pin)?.BindingContext is Models.ShopModel shop)
            {
                await _viewModel.GoToDetailCommand.ExecuteAsync(shop);
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
                TransitionTo(FollowState.Free);
                ScheduleResumeFollow();

                MoveCamera(selectedPoi.Location, 300);
                ((CollectionView)sender).SelectedItem = null; // Reset Selection để có thể bấm lại vào chính quán đó sau này
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Selection error: {ex.Message}");
        }
    }
}