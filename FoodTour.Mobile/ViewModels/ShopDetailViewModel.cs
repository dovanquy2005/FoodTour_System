using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FoodTour.Mobile.Messages;
using FoodTour.Mobile.Models;
using FoodTour.Mobile.Services;

namespace FoodTour.Mobile.ViewModels
{
    [QueryProperty(nameof(Shop), "ShopData")]
    public partial class ShopDetailViewModel : BaseViewModel, IRecipient<LanguageChangedMessage>, IDisposable
    {
        private readonly DatabaseService _dbService;
        private ShopModel? shop;

        public ShopModel? Shop
        {
            get => shop;
            set
            {
                if (SetProperty(ref shop, value) && value != null)
                {
                    LoadDishes(value.Id);
                    RefreshShopDataAsync(value.Id);
                }
            }
        }

        private async void RefreshShopDataAsync(string shopId)
        {
            try
            {
                var freshShop = await _dbService.GetShopAsync(shopId);
                if (freshShop != null && shop != null)
                {
                    // Chỉ cập nhật UI nếu có sự khác biệt (nhất là sau khi ứng dụng đồng bộ text mới ngầm)
                    if (freshShop.Name != shop.Name || freshShop.Description != shop.Description || freshShop.Address != shop.Address)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            // Bỏ qua setter để tránh gọi lại LoadDishes()
                            SetProperty(ref shop, freshShop, nameof(Shop));
                        });
                    }
                }
            }
            catch { }
        }

        [ObservableProperty]
        ObservableCollection<DishModel> dishes = new();

        public ShopDetailViewModel(DatabaseService dbService)
        {
            _dbService = dbService;
            WeakReferenceMessenger.Default.Register(this);
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
                Dishes = new ObservableCollection<DishModel>(data);
            });
        }

        public async void Receive(LanguageChangedMessage message)
        {
            // Nếu trang chi tiết đang mở và có shop hiện tại, reload lại dữ liệu Text / Translation từ DB
            if (shop != null && !string.IsNullOrEmpty(shop.Id))
            {
                try
                {
                    var updatedShop = await _dbService.GetShopAsync(shop.Id);
                    if (updatedShop != null)
                    {
                        // Gắn lại Shop sẽ tự trigger OnPropertyChanged và gọi lại LoadDishes()
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            Shop = updatedShop;
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ShopDetail] Receive LanguageChanged error: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }
    }
}