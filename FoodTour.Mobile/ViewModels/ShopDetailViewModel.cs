using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using FoodTour.Mobile.Models;
using FoodTour.Mobile.Services;

namespace FoodTour.Mobile.ViewModels
{
    [QueryProperty(nameof(Shop), "ShopData")]
    public partial class ShopDetailViewModel : BaseViewModel
    {
        private readonly DatabaseService _dbService;
        private ShopModel? shop;

        public ShopModel? Shop
        {
            get => shop;
            set
            {
                SetProperty(ref shop, value);
                if (value != null) LoadDishes(value.Id);
            }
        }

        public ObservableCollection<DishModel> Dishes { get; } = new();

        public ShopDetailViewModel(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        [RelayCommand]
        public async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }

        private async void LoadDishes(string shopId)
        {
            var data = await _dbService.GetDishesByShopAsync(shopId);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Dishes.Clear();
                foreach (var d in data) Dishes.Add(d);
            });
        }
    }
}