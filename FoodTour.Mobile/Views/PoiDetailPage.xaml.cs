using FoodTour.Mobile.ViewModels;

namespace FoodTour.Mobile.Views;

public partial class PoiDetailPage : ContentPage
{
	// Sửa constructor: Nhận PoiDetailViewModel
	public PoiDetailPage(PoiDetailViewModel vm)
	{
		InitializeComponent();

		// Kết nối giao diện với logic xử lý
		BindingContext = vm;
	}
}