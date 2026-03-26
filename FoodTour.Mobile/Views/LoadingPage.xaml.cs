using FoodTour.Mobile.ViewModels;
namespace FoodTour.Mobile.Views;

public partial class LoadingPage : ContentPage
{
    public LoadingPage(LoadingViewModel vm) // Inject VM vào
    {
        InitializeComponent();
        BindingContext = vm; // Gán ngữ cảnh dữ liệu
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is LoadingViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}