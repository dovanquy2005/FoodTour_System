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
        private List<ShopModel> _allShops = new();

        [ObservableProperty]
        ObservableCollection<ShopModel> shops = new();

        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    FilterShops();
                }
            }
        }

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
                _allShops = data;
                FilterShops();
            });
        }

        private void FilterShops()
        {
            if (string.IsNullOrWhiteSpace(_searchQuery))
            {
                Shops = new ObservableCollection<ShopModel>(_allShops);
            }
            else
            {
                var query = _searchQuery.ToLowerInvariant();
                var filtered = _allShops.Where(s => 
                    (s.Name?.ToLowerInvariant().Contains(query) == true) || 
                    (s.Address?.ToLowerInvariant().Contains(query) == true));
                Shops = new ObservableCollection<ShopModel>(filtered);
            }
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