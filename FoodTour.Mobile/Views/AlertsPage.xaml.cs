using FoodTour.Mobile.ViewModels;

namespace FoodTour.Mobile.Views;

public partial class AlertsPage : ContentPage
{
    public AlertsPage(AlertsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
