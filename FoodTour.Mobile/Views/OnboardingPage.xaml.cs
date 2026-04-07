using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace FoodTour.Mobile.Views;

public partial class OnboardingPage : ContentPage
{
    private readonly Services.ILocalizationService _localizationService;
    private readonly Services.DatabaseService _databaseService;
    private readonly ViewModels.PlayerViewModel _playerVm;
    private readonly Services.WalkingSimulationService _locationService;

    public OnboardingPage(Services.ILocalizationService localizationService, Services.DatabaseService databaseService, ViewModels.PlayerViewModel playerVm, Services.WalkingSimulationService locationService)
    {
        InitializeComponent();
        _localizationService = localizationService;
        _databaseService = databaseService;
        _playerVm = playerVm;
        _locationService = locationService;
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

        // Determine API URL 
        string apiUrl = AppConfig.ApiBaseUrl;

        // REAL SYNC
        bool success = await _databaseService.FullSyncAsync(apiUrl, _localizationService);

        if (success)
        {
            DownloadProgressBar.Progress = 0.5;
            DownloadStatusLabel.Text = _localizationService["Onboarding_DownloadingAssets"] ?? "Đang tải ảnh và âm thanh...";

            await _databaseService.DownloadAllAssetsAsync(apiUrl);

            DownloadProgressBar.Progress = 1.0;
            DownloadStatusLabel.Text = completeText;
            await Task.Delay(1000);

            // Save settings
            Preferences.Default.Set("IsSetupCompleted", true);
            Preferences.Default.Set("IsOfflineMode", true);

            // Navigate to Main App
            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = new AppShell(_playerVm, _locationService, _localizationService, _databaseService);
            }
        }
        else
        {
            DownloadStatusLabel.Text = _localizationService["Onboarding_ErrorConn"] ?? "Lỗi kết nối server.";
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
            Application.Current.Windows[0].Page = new AppShell(_playerVm, _locationService, _localizationService, _databaseService);
        }
    }
}
