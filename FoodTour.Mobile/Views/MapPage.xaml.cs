using FoodTour.Mobile.ViewModels; // Nhớ dòng này để nhận diện được ViewModel

namespace FoodTour.Mobile.Views;

public partial class MapPage : ContentPage
{
    // Sửa constructor: Thêm tham số MapViewModel vm vào trong ngoặc
    public MapPage(MapViewModel vm)
    {
        InitializeComponent();

        // Gán ngữ cảnh dữ liệu: Để giao diện (View) hiểu được dữ liệu từ (ViewModel)
        BindingContext = vm;
    }
}