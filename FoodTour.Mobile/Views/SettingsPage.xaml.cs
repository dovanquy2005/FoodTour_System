using FoodTour.Mobile.ViewModels;

namespace FoodTour.Mobile.Views;

public partial class SettingsPage : ContentPage
{
	// Sửa constructor: Nhận SettingsViewModel
	private bool _isInitializing = true;

	public SettingsPage(SettingsViewModel vm)
	{
		InitializeComponent();

		// Kết nối giao diện với logic xử lý
		BindingContext = vm;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_isInitializing = true;
		DataModeSwitch.IsToggled = Preferences.Default.Get("IsOfflineMode", false);
		_isInitializing = false;
	}

	private async void OnDataModeToggled(object sender, ToggledEventArgs e)
	{
		if (_isInitializing) return;

		if (e.Value) // OFF to ON
		{
			DataModeProgressBarSection.IsVisible = true;
			DataModeSwitch.IsEnabled = false;

			for (int i = 0; i <= 100; i += 2)
			{
				DataModeProgressBar.Progress = i / 100.0;
				DataModeProgressLabel.Text = $"Đang tải... {i}%";
				await Task.Delay(50);
			}

			Preferences.Default.Set("IsOfflineMode", true);
			DataModeProgressBarSection.IsVisible = false;
			DataModeSwitch.IsEnabled = true;

			var appShell = Application.Current?.Windows.FirstOrDefault()?.Page as AppShell;
			appShell?.UpdateStatusIndicator();
		}
		else // ON to OFF
		{
			Preferences.Default.Set("IsOfflineMode", false);
			var appShell = Application.Current?.Windows.FirstOrDefault()?.Page as AppShell;
			appShell?.UpdateStatusIndicator();
		}
	}
}