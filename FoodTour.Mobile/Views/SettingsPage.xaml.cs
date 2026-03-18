using FoodTour.Mobile.ViewModels;

namespace FoodTour.Mobile.Views;

public partial class SettingsPage : ContentPage
{
	// Code-behind giữ gọn: chỉ constructor, mọi logic xử lý nằm trong SettingsViewModel
	public SettingsPage(SettingsViewModel vm)
	{
		InitializeComponent();

		// Kết nối giao diện với ViewModel
		BindingContext = vm;
	}
}