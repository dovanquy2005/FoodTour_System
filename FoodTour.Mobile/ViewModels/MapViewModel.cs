using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoodTour.Mobile.Models;
using Microsoft.Maui.Media;

namespace FoodTour.Mobile.ViewModels;

public partial class MapViewModel : BaseViewModel
{
    [ObservableProperty] ObservableCollection<PoiModel> pois;

    // --- CÁC BIẾN QUẢN LÝ TRẠNG THÁI PLAYER ---

    [ObservableProperty] bool isPlayerVisible = false; // Có hiện Player không? (Vào vùng đỏ mới hiện)

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPlayerCollapsed))] // Khi Expanded thay đổi thì Collapsed cũng đổi theo
    bool isPlayerExpanded = true; // Đang mở to hay thu nhỏ?

    public bool IsPlayerCollapsed => !IsPlayerExpanded; // Biến phụ để Binding giao diện (Ngược lại của Expanded)

    [ObservableProperty] string currentShopName = "";
    [ObservableProperty] string playerStatus = "";
    [ObservableProperty] string playIcon = "⏸";

    private PoiModel _currentPoi;
    private CancellationTokenSource _cts;

    public MapViewModel()
    {
        Pois = new ObservableCollection<PoiModel>();
        LoadData();
    }

    public async Task OnEnterShop(PoiModel shop)
    {
        if (_currentPoi == shop && IsPlayerVisible) return;

        _currentPoi = shop;
        IsPlayerVisible = true;

        // 🔥 QUAN TRỌNG: Khi gặp quán mới thì luôn TỰ ĐỘNG BUNG TO ra để khách thấy
        IsPlayerExpanded = true;

        CurrentShopName = $"Đang phát: {shop.Name}";
        await StartReading();
    }

    // 👇 HÀM MỚI: Bật/Tắt chế độ thu nhỏ
    [RelayCommand]
    void ToggleExpand()
    {
        IsPlayerExpanded = !IsPlayerExpanded;
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

            string content = $"{_currentPoi.Name}. {_currentPoi.Description ?? "Mời bạn ghé thăm."}";
            await TextToSpeech.Default.SpeakAsync(content, options, _cts.Token);
            PlayIcon = "▶";
            PlayerStatus = "Đã kết thúc";
        }
        catch (Exception) { PlayIcon = "▶"; }
    }

    private void LoadData()
    {
        // (Giữ nguyên dữ liệu cũ của bạn)
        Pois.Add(new PoiModel { Name = "Phở Thìn Lò Đúc", Latitude = 21.0185, Longitude = 105.8550, Description = "Phở tái lăn trứ danh, thơm ngon đến từng giọt cuối cùng." });
        Pois.Add(new PoiModel { Name = "Bún Chả Hương Liên", Latitude = 21.0180, Longitude = 105.8520, Description = "Quán Obama đã ăn." });
        Pois.Add(new PoiModel { Name = "Cafe Giảng", Latitude = 21.0343, Longitude = 105.8519, Description = "Cafe trứng nổi tiếng." });
    }
}