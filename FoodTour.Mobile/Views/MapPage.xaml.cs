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
    private readonly WalkingSimulationService _walkingSimulationService;
    private Location? _userLocation = null; // Vị trí thật của người dùng
    private Models.ShopModel? _pendingRouteShop = null; // Quán cần chỉ đường nếu Map chưa vẽ xong
    private bool _isMapLoaded = false; // Flag kiểm tra map render xong lần đầu chưa

    private enum FollowState { Following, Free, RouteView }
    private FollowState _followState = FollowState.Following;

    private const double FollowRadiusMeters = 250; // bán kính follow
    private const double ShopRadiusMeters = 50; // bán kính nhận diện shop
    private DateTime _lastMoveTime = DateTime.MinValue; // thời gian cuối cùng user move map
    private const int MoveThrottleMs = 800; // khoảng thời gian giữa 2 lần check move
    private CancellationTokenSource _resumeCts = new();
    private const int ResumeDelayMs = 30_000; // Tự động resume follow sau 30s
    private readonly Dictionary<string, Circle> _shopCircles = new(); // Key là shop.Id thay vì Object để tránh khác reference

    public MapPage(MapViewModel vm, WalkingSimulationService walkingSimulationService)
    {
        _walkingSimulationService = walkingSimulationService;
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
                // Map đã sẵn sàng, vẽ đường ngay lập tức
                DrawRouteToShop(m.TargetShop);
            else
                // Map chưa khởi tạo xong, lưu lại và vẽ khi Loaded
                _pendingRouteShop = m.TargetShop;
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

            // Edge case: Quay lại tab sau khi load lần đầu
            if (_isMapLoaded && MainMap?.Handler != null)
            {
                MainThread.BeginInvokeOnMainThread(() => DrawRadiusCircles());
            }
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

            _walkingSimulationService.ShopEntered -= OnShopEntered;
            _walkingSimulationService.ShopExited -= OnShopExited;
            
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            if (_viewModel.Shops != null)
                _viewModel.Shops.CollectionChanged -= OnShopsCollectionChanged;

            Geolocation.Default.LocationChanged -= OnUserLocationChanged;
            UnhookAndroid();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MapPage OnDisappearing error: {ex.Message}");
        }
    }

    private void OnShopsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_isMapLoaded || MainMap?.Handler == null) return;
        MainThread.BeginInvokeOnMainThread(() => DrawRadiusCircles());
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_viewModel.Shops))
        {
            if (_viewModel.Shops != null)
            {
                // Re-subscribe to the new collection
                _viewModel.Shops.CollectionChanged -= OnShopsCollectionChanged; // Ensure no duplicate subscription
                _viewModel.Shops.CollectionChanged += OnShopsCollectionChanged;
            }
            
            // Trigger a full redraw
            if (_isMapLoaded && MainMap?.Handler != null)
            {
                MainThread.BeginInvokeOnMainThread(() => DrawRadiusCircles());
            }
        }
    }

    // MAP LOADED
    private async void MainMap_Loaded(object? sender, EventArgs e)
    {
        try
        {
            // Load dữ liệu từ Database nếu danh sách đang trống
            if (_viewModel.Shops == null || _viewModel.Shops.Count == 0)
            {
                await _viewModel.LoadData();
            }

            // Đăng kí CollectionChanged sau khi có Shops
            if (_viewModel.Shops != null)
                _viewModel.Shops.CollectionChanged += OnShopsCollectionChanged;

            // Đăng ký PropertyChanged để bắt sự kiện gán mới Shops
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            if (MainMap != null)
                MainMap.IsShowingUser = true;

            // Hook gesture dectector của native map platform
            HookAndroid();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MainMap_Loaded error: {ex.Message}");
        }
    }

    /// <summary>
    /// NATIVE GESTURE HOOK. Hook xuống native Google Maps để dùng các listener chính xác hơn.
    /// Do MAUI Maps abstraction không expose "user dragged map" event.
    /// </summary>
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

                    // Detect đúng gesture của user
                    _googleMap.SetOnCameraMoveStartedListener(new CameraMoveStartedCallback(reason =>
                    {
                        if (reason == Android.Gms.Maps.GoogleMap.OnCameraMoveStartedListener.ReasonGesture)
                            TransitionTo(FollowState.Free);
                    }));

                    // Timer resume bắt đầu đếm khi user thả tay
                    _googleMap.SetOnCameraIdleListener(new CameraIdleCallback(() =>
                    {
                        if (_followState == FollowState.Free)
                            ScheduleResumeFollow();
                    }));

                    // Nuốt POI click của Google Maps
                    _googleMap.SetOnPoiClickListener(new PoiClickCallback(poi =>
                    {
                        if (_viewModel.Shops == null) return;

                        var matched = _viewModel.Shops.FirstOrDefault(shop =>
                            Location.CalculateDistance(
                                new Location(poi.LatLng.Latitude, poi.LatLng.Longitude),
                                shop.Location,
                                DistanceUnits.Kilometers) * 1000 < 50);

                        if (matched != null)
                            MainThread.BeginInvokeOnMainThread(async () =>
                                await _viewModel.GoToDetailCommand.ExecuteAsync(matched));
                    }));

                    //Biết chính xác khi map render xong lần đầu
                    _googleMap.SetOnMapLoadedCallback(new MapLoadedCallback(() =>
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            if (_isMapLoaded) return;
                            _isMapLoaded = true;
                            DrawRadiusCircles();

                            if (_pendingRouteShop != null)
                            {
                                DrawRouteToShop(_pendingRouteShop);
                                _pendingRouteShop = null;
                            }
                            else if (_userLocation != null)
                            {
                                TransitionTo(FollowState.Following);
                                MoveCamera(_userLocation, FollowRadiusMeters);
                            }
                        });
                    }));

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (_isMapLoaded) return; // guard tránh chạy 2 lần nếu callback kịp fire
                        _isMapLoaded = true;
                        DrawRadiusCircles();

                        if (_pendingRouteShop != null)
                        {
                            DrawRouteToShop(_pendingRouteShop);
                            _pendingRouteShop = null;
                        }
                        else if (_userLocation != null)
                        {
                            TransitionTo(FollowState.Following);
                            MoveCamera(_userLocation, FollowRadiusMeters);
                        }
                    });
                }));
            }
        }
        catch (Exception ex) { Console.WriteLine($"HookAndroid error: {ex.Message}"); }
    }

    private void UnhookAndroid()
    {
        if (_googleMap != null)
        {
            _googleMap.SetOnCameraMoveStartedListener(null);
            _googleMap.SetOnCameraIdleListener(null);
            _googleMap.SetOnPoiClickListener(null);
            _googleMap.SetOnMapLoadedCallback(null);
            _googleMap = null;
        }
    }

    private class MapReadyCallback(Action<Android.Gms.Maps.GoogleMap> cb) : Java.Lang.Object, Android.Gms.Maps.IOnMapReadyCallback
    { public void OnMapReady(Android.Gms.Maps.GoogleMap g) => cb(g); }

    private class CameraMoveStartedCallback(Action<int> cb) : Java.Lang.Object, Android.Gms.Maps.GoogleMap.IOnCameraMoveStartedListener
    { public void OnCameraMoveStarted(int reason) => cb(reason); }

    private class CameraIdleCallback(Action cb) : Java.Lang.Object, Android.Gms.Maps.GoogleMap.IOnCameraIdleListener
    { public void OnCameraIdle() => cb(); }

    private class PoiClickCallback(Action<Android.Gms.Maps.Model.PointOfInterest> cb) : Java.Lang.Object, Android.Gms.Maps.GoogleMap.IOnPoiClickListener
    { public void OnPoiClick(Android.Gms.Maps.Model.PointOfInterest poi) => cb(poi); }

    private class MapLoadedCallback(Action cb) : Java.Lang.Object, Android.Gms.Maps.GoogleMap.IOnMapLoadedCallback
    { public void OnMapLoaded() => cb(); }

    // STATE MACHINE
    private void TransitionTo(FollowState newState) => _followState = newState;

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

            // Đăng ký sự kiện enter/exit zone từ WalkingSimulationService
            _walkingSimulationService.ShopEntered -= OnShopEntered;
            _walkingSimulationService.ShopEntered += OnShopEntered;
            _walkingSimulationService.ShopExited -= OnShopExited;
            _walkingSimulationService.ShopExited += OnShopExited;

            // Đăng ký nhận thông tin vị trí mới (Lắng nghe Foreground đã được khởi chạy ở Global Service)
            Geolocation.Default.LocationChanged -= OnUserLocationChanged;
            Geolocation.Default.LocationChanged += OnUserLocationChanged;

            // FIX: Cố gắng lấy vị trí lần cuối cùng ngay lập tức để hiển thị liền mà không bắt Tab đợi 2-3s
            var location = await Geolocation.Default.GetLastKnownLocationAsync();
            if (location == null)
            {
                // Fallback nếu không có last known location
                location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(2)));
            }

            if (location != null)
            {
                _userLocation = location;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (MainMap?.Handler != null)
                    {
                        if (_pendingRouteShop != null)
                        {
                            DrawRouteToShop(_pendingRouteShop);
                            _pendingRouteShop = null;
                        }
                        else
                        {
                            TransitionTo(FollowState.Following);
                            MoveCamera(location, FollowRadiusMeters);
                        }
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
            if (userLoc.Accuracy.HasValue && userLoc.Accuracy.Value > 25.0) return; // Lọc GPS kém chính xác

            _userLocation = userLoc;

            // Camera follow
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
                if (_userLocation == null) 
                {
                    _pendingRouteShop = targetShop;
                    return;
                }

                TransitionTo(FollowState.RouteView);
                ScheduleResumeFollow();

                foreach (var line in MainMap.MapElements.OfType<Polyline>().ToList())
                    MainMap.MapElements.Remove(line);

                var polyline = new Polyline
                {
                    StrokeColor = Colors.Blue,
                    StrokeWidth = 8,
                    Geopath = { _userLocation, targetShop.Location }
                };
                MainMap.MapElements.Add(polyline);

                // Tự động xóa đường màu xanh sau 3 giây để tránh làm rối bản đồ
                Task.Delay(TimeSpan.FromSeconds(3)).ContinueWith(_ =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (MainMap != null && MainMap.MapElements.Contains(polyline))
                        {
                            MainMap.MapElements.Remove(polyline);
                        }
                    });
                });

                double distKm = Location.CalculateDistance(_userLocation, targetShop.Location, DistanceUnits.Kilometers);
                var center = new Location(
                    (_userLocation.Latitude + targetShop.Location.Latitude) / 2,
                    (_userLocation.Longitude + targetShop.Location.Longitude) / 2);
                MoveCamera(center, Math.Max(distKm * 1000 * 0.6, 300));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DrawRoute error: {ex.Message}");
            }
        });
    }

    // HELPERS
    private void MoveCamera(Location location, double radiusMeters) =>
        MainMap?.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(radiusMeters)));

    private static double DistanceMeters(Location a, Location b) =>
        Location.CalculateDistance(a, b, DistanceUnits.Kilometers) * 1000;

    private void DrawRadiusCircles()
    {
        if (MainMap == null) return;

        var existingPolylines = MainMap.MapElements.OfType<Polyline>().ToList();

        MainMap.MapElements.Clear();
        MainMap.Pins.Clear();
        _shopCircles.Clear();

        foreach (var p in existingPolylines)
        {
            MainMap.MapElements.Add(p);
        }

        if (_viewModel.Shops == null || _viewModel.Shops.Count == 0) return;

        foreach (var shop in _viewModel.Shops)
        {
            try
            {
                // Kiểm tra xem quán này có đang active (đang phát audio) không
                bool isCurrent = _walkingSimulationService.CurrentShop?.Id == shop.Id;

                // Vẽ vòng tròn bán kính động theo database (nếu chưa set thì mặc định 50m)
                double radius = shop.Radius > 0 ? shop.Radius : 50.0;
                var circle = new Circle
                {
                    Center = shop.Location,
                    Radius = Distance.FromMeters(radius),
                    StrokeColor = isCurrent ? Colors.Green : Colors.Red,
                    StrokeWidth = 2,
                    FillColor = isCurrent ? Colors.Green.WithAlpha(0.25f) : Colors.Red.WithAlpha(0.25f)
                };
                MainMap.MapElements.Add(circle);
                _shopCircles[shop.Id] = circle;

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
            if (e.CurrentSelection.FirstOrDefault() is not Models.ShopModel selectedPoi) return;
            TransitionTo(FollowState.Free);
            ScheduleResumeFollow();

            MoveCamera(selectedPoi.Location, 300);
            ((CollectionView)sender).SelectedItem = null; // Reset Selection để có thể bấm lại vào chính quán đó sau này
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Selection error: {ex.Message}");
        }
    }

    private void HighlightShopCircle(Models.ShopModel shop)
    {
        if (shop == null || string.IsNullOrEmpty(shop.Id)) return;
        if (!_shopCircles.TryGetValue(shop.Id, out var circle)) return;
        circle.StrokeColor = Colors.Green;
        circle.FillColor = Colors.Green.WithAlpha(0.25f);
    }

    private void ResetShopCircle(Models.ShopModel shop)
    {
        if (shop == null || string.IsNullOrEmpty(shop.Id)) return;
        if (!_shopCircles.TryGetValue(shop.Id, out var circle)) return;
        circle.StrokeColor = Colors.Red;
        circle.FillColor = Colors.Red.WithAlpha(0.25f);
    }
    
    private void OnShopEntered(Models.ShopModel shop)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            HighlightShopCircle(shop);
            try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { }
        });
    }

    private void OnShopExited(Models.ShopModel shop)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ResetShopCircle(shop);
        });
    }
}