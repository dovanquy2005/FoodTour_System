using FoodTour.Mobile.Views;

namespace FoodTour.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Đăng ký route cho trang chi tiết để có thể gọi: GoToAsync("PoiDetail")
        Routing.RegisterRoute(nameof(PoiDetailPage), typeof(PoiDetailPage));
    }
}