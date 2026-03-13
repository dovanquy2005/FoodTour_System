using FoodTour.Mobile.Models;
using FoodTour.Mobile.ViewModels;
using FoodTour.Mobile.Messages;
using CommunityToolkit.Mvvm.Messaging;

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

    private async void OnShopCardTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is ShopModel shop)
        {
            await _viewModel.GoToDetailCommand.ExecuteAsync(shop);
        }
    }

    private async void OnNavigateToShopTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is ShopModel shop)
        {
            try
            {
                // Gửi message trước khi chuyển tab
                WeakReferenceMessenger.Default.Send(new RouteToShopMessage(shop));

                // Chuyển sang tab Map
                await Shell.Current.GoToAsync("//MapPage");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Navigate error: {ex.Message}");
            }
        }
    }
}