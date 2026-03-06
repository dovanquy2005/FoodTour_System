using FoodTour.Mobile.ViewModels;

namespace FoodTour.Mobile.Views;

public partial class ShopDetailPage : ContentPage
{
	// Sửa constructor: Nhận ShopDetailViewModel
	public ShopDetailPage(ShopDetailViewModel vm)
	{
		InitializeComponent();

		// Kết nối giao diện với logic xử lý
		BindingContext = vm;
	}
}