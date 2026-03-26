using FoodTour.Mobile.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Networking;
using Microsoft.Maui.ApplicationModel;

namespace FoodTour.Mobile;

public partial class AppShell : Shell
{
    public AppShell(ViewModels.PlayerViewModel playerVm, Services.WalkingSimulationService locationService)
    {
        InitializeComponent();
        BindingContext = playerVm;

        // Đăng ký route cho trang chi tiết
        Routing.RegisterRoute(nameof(Views.ShopDetailPage), typeof(Views.ShopDetailPage));
        Routing.RegisterRoute(nameof(Views.LanguageSelectionPage), typeof(Views.LanguageSelectionPage));

        // Đăng ký sự kiện lắng nghe kết nối mạng
        Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;

        // Bắt đầu dịch vụ GPS ngầm toàn cục
        _ = locationService.Start();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateStatusIndicator();
    }

    private void Connectivity_ConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateStatusIndicator();
        });
    }

    // Cập nhật chỉ báo trạng thái mạng — đã loại bỏ logic "Offline Mode" cũ
    // Chỉ hiển thị trạng thái kết nối mạng thực tế
    public void UpdateStatusIndicator()
    {
        var currentAccess = Connectivity.Current.NetworkAccess;

        if (currentAccess == NetworkAccess.Internet)
        {
            // Có kết nối mạng → ẩn chỉ báo
            StatusIndicatorContainer.IsVisible = false;
        }
        else
        {
            // Không có mạng → hiển thị cảnh báo mất kết nối
            StatusIcon.Text = "❌";
            StatusText.Text = "No Connection";
            StatusText.TextColor = Colors.Red;
            StatusIndicatorContainer.IsVisible = true;
        }
    }

    ~AppShell()
    {
        Connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;
    }
}