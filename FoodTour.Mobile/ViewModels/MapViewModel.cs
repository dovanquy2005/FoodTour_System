using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FoodTour.Mobile.Messages;
using FoodTour.Mobile.Models;
using Microsoft.Maui.Media;
using FoodTour.Mobile.Services;
using FoodTour.Mobile.Views;

namespace FoodTour.Mobile.ViewModels;

public partial class MapViewModel : BaseViewModel, IRecipient<LanguageChangedMessage>, IDisposable
{
    private readonly DatabaseService _dbService;

    [ObservableProperty] ObservableCollection<ShopModel> shops;

    public MapViewModel(DatabaseService dbService)
    {
        _dbService = dbService;
        Shops = new ObservableCollection<ShopModel>();
        WeakReferenceMessenger.Default.Register(this);
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
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Shops = new ObservableCollection<ShopModel>(data);
        });
    }

    public async void Receive(LanguageChangedMessage message)
    {
        // Khi ngôn ngữ thay đổi, tải lại toàn bộ Shops từ DB để bốc được bản dịch mới nhất,
        // từ đó kích hoạt CollectionChanged trên View làm vẽ lại toàn bộ Pin bản đồ (cọc đỏ).
        await LoadData();
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}