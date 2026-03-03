namespace FoodTour.Mobile.Views;

public partial class ExplorePage : ContentPage
{
    // Bơm ExploreViewModel vào qua constructor
    public ExplorePage(ViewModels.ExploreViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel; // Nối ViewModel với Giao diện
    }
}