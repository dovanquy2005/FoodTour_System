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

    [ObservableProperty] ObservableCollection<ShopModel> shops;

    public MapViewModel(DatabaseService dbService)
    {
        _dbService = dbService;
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