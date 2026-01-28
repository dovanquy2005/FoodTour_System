using FoodTour.Mobile.ViewModels;

namespace FoodTour.Mobile.Views;

public partial class SettingsPage : ContentPage
{
	// Sửa constructor: Nhận SettingsViewModel
	public SettingsPage(SettingsViewModel vm)
	{
		InitializeComponent();

		// Kết nối giao diện với logic xử lý
		BindingContext = vm;
	}
}