using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoodTour.Mobile.Models;
using Microsoft.Maui.Media;
using FoodTour.Mobile.Services; // ✅ Đã có namespace này
using FoodTour.Mobile.Views;    // ✅ Thêm cái này để chuyển trang (PoiDetailPage)

namespace FoodTour.Mobile.ViewModels;

public partial class MapViewModel : BaseViewModel
{
    private readonly DatabaseService _dbService;
    private CancellationTokenSource _cts = new CancellationTokenSource();
    private PoiModel? _currentPoi;

    [ObservableProperty] ObservableCollection<PoiModel> pois;

    // --- CÁC BIẾN PLAYER ---
    [ObservableProperty] bool isPlayerVisible = false;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsPlayerCollapsed))] bool isPlayerExpanded = true;
    public bool IsPlayerCollapsed => !IsPlayerExpanded;

    [ObservableProperty] string currentShopName = "";
    [ObservableProperty] string playerStatus = "";
    [ObservableProperty] string playIcon = "⏸";

    public MapViewModel(DatabaseService dbService)
    {
        _dbService = dbService;
        Pois = new ObservableCollection<PoiModel>();

        // Gọi hàm load dữ liệu async
        Task.Run(async () => await LoadData());
    }

    // 👇 1. HÀM CHUYỂN TRANG (Đã bổ sung lại)
    [RelayCommand]
    async Task GoToDetail(PoiModel poi)
    {
        if (poi == null) return;

        // Chuyển sang trang Detail và gửi kèm dữ liệu
        await Shell.Current.GoToAsync(nameof(PoiDetailPage), new Dictionary<string, object>
        {
            { "PoiData", poi }
        });
    }

    // 👇 2. LOGIC PLAYER & GPS
    public async Task OnEnterShop(PoiModel shop)
    {
        if (_currentPoi == shop && IsPlayerVisible) return;

        _currentPoi = shop;
        IsPlayerVisible = true;
        IsPlayerExpanded = true;
        CurrentShopName = $"Đang phát: {shop.Name}";
        await StartReading();
    }

    [RelayCommand]
    void ToggleExpand() => IsPlayerExpanded = !IsPlayerExpanded;

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

    private async Task StartReading()
    {
        PlayIcon = "⏸";
        PlayerStatus = "Đang thuyết minh...";
        if (_cts != null) _cts.Cancel();
        _cts = new CancellationTokenSource();

        try
        {
            var locales = await TextToSpeech.Default.GetLocalesAsync();
            var vnLocale = locales.FirstOrDefault(x => x.Language == "vi");
            var options = new SpeechOptions { Locale = vnLocale, Pitch = 1.0f, Volume = 1.0f };

            // Thêm check null cho an toàn
            string name = _currentPoi?.Name ?? "";
            string desc = _currentPoi?.Description ?? "Mời bạn ghé thăm.";
            string content = $"{name}. {desc}";

            await TextToSpeech.Default.SpeakAsync(content, options, _cts.Token);
            PlayIcon = "▶";
            PlayerStatus = "Đã kết thúc";
        }
        catch (Exception) { PlayIcon = "▶"; }
    }

    // 👇 3. LOGIC LOAD DATABASE
    [RelayCommand] // Thêm Command này để có thể gọi lại từ UI nếu cần (ví dụ nút Refresh)
    public async Task LoadData()
    {
        // Lấy dữ liệu thật từ SQLite
        var data = await _dbService.GetPoisAsync();

        // Cập nhật lên UI (MainThread)
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Pois.Clear();
            foreach (var item in data)
            {
                Pois.Add(item);
            }
        });
    }
}