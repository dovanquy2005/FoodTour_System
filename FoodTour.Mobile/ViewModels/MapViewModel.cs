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
    private CancellationTokenSource _cts = new CancellationTokenSource();
    private ShopModel? _currentShop;

    [ObservableProperty] ObservableCollection<ShopModel> shops;

    // --- CÁC BIẾN PLAYER ---
    [ObservableProperty] bool isPlayerVisible = false;
    [ObservableProperty] string currentShopName = "";
    [ObservableProperty] string currentShopImage = "";
    [ObservableProperty] string playerStatus = "";
    [ObservableProperty] string playIcon = "⏸";

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

    // 👇 2. LOGIC PLAYER & GPS
    public async Task OnEnterShop(ShopModel shop)
    {
        if (_currentShop == shop && IsPlayerVisible) return;

        _currentShop = shop;
        IsPlayerVisible = true;
        CurrentShopName = shop.Name;
        CurrentShopImage = shop.ImageUrl ?? "";
        PlayerStatus = "Đang thuyết minh...";
        await StartReading();
    }

    [RelayCommand]
    async Task PlayPause()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
            PlayIcon = "▶";
            PlayerStatus = "Đã tạm dừng";
        }
        else
        {
            await StartReading();
        }
    }

    [RelayCommand]
    void SkipNext()
    {
        if (Shops == null || Shops.Count == 0 || _currentShop == null) return;
        int idx = Shops.IndexOf(_currentShop);
        int nextIdx = (idx + 1) % Shops.Count;
        _ = OnEnterShop(Shops[nextIdx]);
    }

    [RelayCommand]
    void SkipPrevious()
    {
        if (Shops == null || Shops.Count == 0 || _currentShop == null) return;
        int idx = Shops.IndexOf(_currentShop);
        int prevIdx = (idx - 1 + Shops.Count) % Shops.Count;
        _ = OnEnterShop(Shops[prevIdx]);
    }

    private async Task StartReading()
    {
        PlayIcon = "⏸";
        PlayerStatus = "Đang thuyết minh...";

        if (_cts != null)
            _cts.Cancel();

        _cts = new CancellationTokenSource();

        try
        {
            var locales = await TextToSpeech.Default.GetLocalesAsync();

            var vnLocale = locales.FirstOrDefault(l =>
                l.Language == "vi" && l.Country == "VN");

            if (vnLocale == null)
                vnLocale = locales.FirstOrDefault(l => l.Language == "vi");

            var options = new SpeechOptions
            {
                Locale = vnLocale,
                Pitch = 0.9f,
                Volume = 1.0f
            };

            string name = _currentShop?.Name ?? "";
            string desc = _currentShop?.Description ?? "Mời bạn ghé thăm.";

            string content = $"{name}. {desc}"
                .Replace("\n", ". ")
                .Replace("  ", " ");

            await TextToSpeech.Default.SpeakAsync(content, options, _cts.Token);

            PlayIcon = "▶";
            PlayerStatus = "Đã kết thúc";
        }
        catch
        {
            PlayIcon = "▶";
        }
    }

    // private async Task StartReading()
    // {
    //     PlayIcon = "⏸";
    //     PlayerStatus = "Đang thuyết minh...";
    //     if (_cts != null) _cts.Cancel();
    //     _cts = new CancellationTokenSource();

    //     try
    //     {
    //         var locales = await TextToSpeech.Default.GetLocalesAsync();
    //         var vnLocale = locales.FirstOrDefault(x => x.Language == "vi");
    //         var options = new SpeechOptions { Locale = vnLocale, Pitch = 1.0f, Volume = 1.0f };

    //         string name = _currentShop?.Name ?? "";
    //         string desc = _currentShop?.Description ?? "Mời bạn ghé thăm.";
    //         string content = $"{name}. {desc}";

    //         await TextToSpeech.Default.SpeakAsync(content, options, _cts.Token);
    //         PlayIcon = "▶";
    //         PlayerStatus = "Đã kết thúc";
    //     }
    //     catch (Exception) { PlayIcon = "▶"; }
    // }

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