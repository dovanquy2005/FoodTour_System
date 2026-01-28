using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using FoodTour.Mobile.Views;
using FoodTour.Mobile.ViewModels;

namespace FoodTour.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiMaps() // <--- QUAN TRỌNG: Để hiển thị bản đồ
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Đăng ký View và ViewModel
        builder.Services.AddSingleton<LoadingPage>();
        builder.Services.AddSingleton<LoadingViewModel>();

        builder.Services.AddSingleton<MapPage>();
        builder.Services.AddSingleton<MapViewModel>();

        builder.Services.AddTransient<PoiDetailPage>();
        builder.Services.AddTransient<PoiDetailViewModel>();

        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<SettingsViewModel>();

        return builder.Build();
    }
}