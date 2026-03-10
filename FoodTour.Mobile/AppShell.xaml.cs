using FoodTour.Mobile.Views;

namespace FoodTour.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Đăng ký route cho trang chi tiết để có thể gọi: GoToAsync("ShopDetail")
        Routing.RegisterRoute(nameof(ShopDetailPage), typeof(ShopDetailPage));

        // Register route for LanguageSelectionPage
        Routing.RegisterRoute(nameof(LanguageSelectionPage), typeof(LanguageSelectionPage));
    }
}