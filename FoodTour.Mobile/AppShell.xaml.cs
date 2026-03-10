using FoodTour.Mobile.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Networking;
using Microsoft.Maui.ApplicationModel;

namespace FoodTour.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        // Đăng ký route cho trang chi tiết
        Routing.RegisterRoute(nameof(Views.ShopDetailPage), typeof(Views.ShopDetailPage));
        Routing.RegisterRoute(nameof(Views.LanguageSelectionPage), typeof(Views.LanguageSelectionPage));

        // Đăng ký sự kiện lắng nghe kết nối mạng
        Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
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

    public void UpdateStatusIndicator()
    {
        var isOfflineMode = Preferences.Default.Get("IsOfflineMode", false);
        var currentAccess = Connectivity.Current.NetworkAccess;

        if (isOfflineMode)
        {
            // Offline Mode ON: Green Cloud with a Tick + text "Offline Mode"
            StatusIcon.Text = "✅"; // Can use an image/font icon for cloud+tick
            StatusText.Text = "Offline Mode";
            StatusText.TextColor = Colors.LimeGreen;
            StatusIndicatorContainer.IsVisible = true;
        }
        else
        {
            if (currentAccess == NetworkAccess.Internet)
            {
                // Offline Mode OFF & Connected: Hide indicator or subtle Wi-Fi icon
                StatusIndicatorContainer.IsVisible = false;
            }
            else
            {
                // Offline Mode OFF & Disconnected: Red Wi-Fi slashed + text "No Connection"
                StatusIcon.Text = "❌"; // Red cross or slashed wifi icon
                StatusText.Text = "No Connection";
                StatusText.TextColor = Colors.Red;
                StatusIndicatorContainer.IsVisible = true;
            }
        }
    }

    ~AppShell()
    {
        Connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;
    }
}