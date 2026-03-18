using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoodTour.Mobile.Models;
using Microsoft.Maui.Media;
using FoodTour.Mobile.Services;
using FoodTour.Mobile.Views;

namespace FoodTour.Mobile.ViewModels;

public partial class MapViewModel : BaseViewModel
{
    private readonly DatabaseService _dbService;
    private readonly IAudioPlayerService _audioService;
    private ShopModel? _currentShop;

    [ObservableProperty] ObservableCollection<ShopModel> shops;

    public MapViewModel(DatabaseService dbService, IAudioPlayerService audioService)
    {
        _dbService = dbService;
        _audioService = audioService;
        Shops = new ObservableCollection<ShopModel>();
    }

    // 👇 1. HÀM CHUYỂN TRANG
    [RelayCommand]
    async Task GoToDetail(ShopModel shop)
    {
        if (shop == null) return;
        await Shell.Current.GoToAsync(nameof(ShopDetailPage), new Dictionary<string, object>
        {
            { "ShopData", shop }
        });
    }

    // 👇 2. LOGIC PLAYER & GPS

    /// <summary>
    /// Bắt đầu thuyết minh shop.
    /// </summary>
    public async Task OnEnterShop(ShopModel shop)
    {
        _currentShop = shop;
        await _audioService.PlayShopAsync(shop);
    }

    /// <summary>
    /// Dừng thuyết minh khi người dùng ra khỏi bán kính shop.
    /// Được gọi bởi OnExitShop của WalkingSimulationService.
    /// </summary>
    public void OnExitShop()
    {
        _audioService.Stop();
        _currentShop = null;
    }

    [RelayCommand]
    async Task SkipNext()
    {
        if (Shops == null || Shops.Count == 0 || _currentShop == null) return;
        int idx = Shops.IndexOf(_currentShop);
        int nextIdx = (idx + 1) % Shops.Count;
        await OnEnterShop(Shops[nextIdx]);
    }

    [RelayCommand]
    async Task SkipPrevious()
    {
        if (Shops == null || Shops.Count == 0 || _currentShop == null) return;
        int idx = Shops.IndexOf(_currentShop);
        int prevIdx = (idx - 1 + Shops.Count) % Shops.Count;
        await OnEnterShop(Shops[prevIdx]);
    }

    // 👇 3. LOGIC LOAD DATABASE
    [RelayCommand]
    public async Task LoadData()
    {
        var data = await _dbService.GetShopsAsync();
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Shops = new ObservableCollection<ShopModel>(data);
        });
    }
}