using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoodTour.Mobile.Models;
using FoodTour.Mobile.Services;
using FoodTour.Mobile.Views;
using CommunityToolkit.Mvvm.Messaging;

namespace FoodTour.Mobile.ViewModels
{
    public partial class ExploreViewModel : BaseViewModel
    {
        private readonly DatabaseService _dbService;

        [ObservableProperty]
        ObservableCollection<ShopModel> shops = new();

        [ObservableProperty]
        private bool isOffline;

        public ExploreViewModel(DatabaseService dbService)
        {
            _dbService = dbService;
            IsOffline = Preferences.Default.Get("IsOfflineMode", false);
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

        [RelayCommand]
        async Task NavigateToShop(ShopModel shop)
        {
            if (shop == null) return;

            // Gửi message sang MapPage
            CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Send(new Messages.RouteToShopMessage(shop));

            // Chuyển sang tab Map (có route name là MainTabs)
            await Shell.Current.GoToAsync("//MainTabs/MapPage");
        }
    }
}