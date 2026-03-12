using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoodTour.Mobile.Models;
using FoodTour.Mobile.Services;
using FoodTour.Mobile.Views;

namespace FoodTour.Mobile.ViewModels
{
    public partial class ExploreViewModel : BaseViewModel
    {
        private readonly DatabaseService _dbService;

        [ObservableProperty]
        ObservableCollection<ShopModel> shops = new();

        public ExploreViewModel(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        [RelayCommand]
        public async Task LoadShops()
        {
            var data = await _dbService.GetShopsAsync();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Shops = new ObservableCollection<ShopModel>(data);
            });
        }

        [RelayCommand]
        async Task GoToDetail(ShopModel shop)
        {
            if (shop == null) return;
            await Shell.Current.GoToAsync(nameof(ShopDetailPage), new Dictionary<string, object>
            {
                { "ShopData", shop }
            });
        }
    }
}