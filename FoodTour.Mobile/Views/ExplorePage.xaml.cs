using FoodTour.Mobile.Models;
using FoodTour.Mobile.ViewModels;

namespace FoodTour.Mobile.Views;

public partial class ExplorePage : ContentPage
{
    private readonly ExploreViewModel _viewModel;

    public ExplorePage(ExploreViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadShops();
    }

    private async void OnShopSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ShopModel shop)
        {
            await _viewModel.GoToDetailCommand.ExecuteAsync(shop);
            ((CollectionView)sender).SelectedItem = null;
        }
    }
}