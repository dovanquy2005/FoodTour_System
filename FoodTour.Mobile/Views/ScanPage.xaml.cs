using FoodTour.Mobile.ViewModels;

namespace FoodTour.Mobile.Views;

public partial class ScanPage : ContentPage
{
    public ScanPage(ScanViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
