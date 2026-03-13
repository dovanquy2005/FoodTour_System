using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace FoodTour.Mobile.Views;

public partial class OnboardingPage : ContentPage
{
    private readonly Services.ILocalizationService _localizationService;
    private readonly Services.DatabaseService _databaseService;

    public OnboardingPage(Services.ILocalizationService localizationService, Services.DatabaseService databaseService)
    {
        InitializeComponent();
        _localizationService = localizationService;
        _databaseService = databaseService;
    }

    private async void OnDownloadOfflineClicked(object sender, EventArgs e)
    {
        // Hide buttons, show progress
        ButtonsArea.IsVisible = false;
        ProgressArea.IsVisible = true;

        var downloadingTextTemplate = _localizationService["Onboarding_Downloading"] ?? "Đang tải dữ liệu... {0}%";
        var completeText = _localizationService["Onboarding_DownloadComplete"] ?? "Tải xuống hoàn tất!";

        DownloadStatusLabel.Text = _localizationService["Onboarding_StartingDownload"] ?? "Đang bắt đầu tải...";
        DownloadProgressBar.Progress = 0.1;

        // Determine API URL (Emulator uses 10.0.2.2, Windows uses localhost)
        string apiUrl = DeviceInfo.Platform == DevicePlatform.Android 
            ? "http://10.0.2.2:5154" 
            : "http://localhost:5154";

        // REAL SYNC
        bool success = await _databaseService.FullSyncAsync(apiUrl, _localizationService);

        if (success)
        {
            DownloadProgressBar.Progress = 1.0;
            DownloadStatusLabel.Text = completeText;
            await Task.Delay(1000);

            // Save settings
            Preferences.Default.Set("IsSetupCompleted", true);
            Preferences.Default.Set("IsOfflineMode", true);

            // Navigate to Main App
            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = new AppShell();
            }
        }
        else
        {
            DownloadStatusLabel.Text = "Lỗi kết nối server. Vui lòng bật server admin.";
            await Task.Delay(3000);
            ButtonsArea.IsVisible = true;
            ProgressArea.IsVisible = false;
        }
    }

    private void OnListenOnlineClicked(object sender, EventArgs e)
    {
        // Save settings
        Preferences.Default.Set("IsSetupCompleted", true);
        Preferences.Default.Set("IsOfflineMode", false);

        // Navigate to Main App
        if (Application.Current?.Windows.Count > 0)
        {
            Application.Current.Windows[0].Page = new AppShell();
        }
    }
}
