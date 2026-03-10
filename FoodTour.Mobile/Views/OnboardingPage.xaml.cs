using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace FoodTour.Mobile.Views;

public partial class OnboardingPage : ContentPage
{
    private readonly Services.ILocalizationService _localizationService;

    public OnboardingPage(Services.ILocalizationService localizationService)
    {
        InitializeComponent();
        _localizationService = localizationService;
    }

    private async void OnDownloadOfflineClicked(object sender, EventArgs e)
    {
        // Hide buttons, show progress
        ButtonsArea.IsVisible = false;
        ProgressArea.IsVisible = true;

        var downloadingTextTemplate = _localizationService["Onboarding_Downloading"] ?? "Đang tải... {0}%";
        var completeText = _localizationService["Onboarding_DownloadComplete"] ?? "Tải xuống hoàn tất!";

        // Simulate file download
        for (int i = 0; i <= 100; i += 2)
        {
            DownloadProgressBar.Progress = i / 100.0;
            DownloadStatusLabel.Text = string.Format(downloadingTextTemplate, i);
            await Task.Delay(50);
        }

        // Complete
        DownloadStatusLabel.Text = completeText;
        await Task.Delay(500);

        // Save settings
        Preferences.Default.Set("IsSetupCompleted", true);
        Preferences.Default.Set("IsOfflineMode", true);

        // Navigate to Main App
        if (Application.Current?.Windows.Count > 0)
        {
            Application.Current.Windows[0].Page = new AppShell();
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
