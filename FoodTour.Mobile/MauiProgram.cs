using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using FoodTour.Mobile.Views;
using FoodTour.Mobile.ViewModels;
using FoodTour.Mobile.Services;
namespace FoodTour.Mobile;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiMaps()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
        // Đăng ký cho tab Explore (Khám phá)
        builder.Services.AddTransient<ExplorePage>();
        builder.Services.AddTransient<ExploreViewModel>();
        // Đăng ký cho tab Map (Bản đồ) 
        builder.Services.AddTransient<MapPage>();
        builder.Services.AddTransient<MapViewModel>();
        // Đăng ký cho tab Scan/QR (Quét mã)
        builder.Services.AddTransient<ScanPage>();
        builder.Services.AddTransient<ScanViewModel>();
        // Đăng ký cho tab Alerts (Thông báo)
        builder.Services.AddTransient<AlertsPage>();
        builder.Services.AddTransient<AlertsViewModel>();
        // Đăng ký cho tab Settings (Cài đặt) - (Đã có sẵn, giữ nguyên)
        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<SettingsViewModel>();
        // Đăng ký View và ViewModel, DATABASE
        builder.Services.AddSingleton<LoadingPage>();
        builder.Services.AddSingleton<LoadingViewModel>();
        builder.Services.AddTransient<ShopDetailPage>();
        builder.Services.AddTransient<ShopDetailViewModel>();
        builder.Services.AddSingleton<DatabaseService>();
        return builder.Build();
    }
}