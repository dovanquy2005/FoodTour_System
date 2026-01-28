using FoodTour.Mobile.ViewModels;
namespace FoodTour.Mobile.Views;

public partial class LoadingPage : ContentPage
{
    public LoadingPage(LoadingViewModel vm) // Inject VM vào
    {
        InitializeComponent();
        BindingContext = vm; // Gán ngữ cảnh dữ liệu
    }
}