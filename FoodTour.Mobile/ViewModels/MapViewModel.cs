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
    private CancellationTokenSource _ttsCts = new CancellationTokenSource(); // dùng riêng cho từng lần SpeakAsync, hủy khi cần dừng TTS
    private CancellationTokenSource _playCts = new CancellationTokenSource(); // dùng để kiểm soát trạng thái Play/Pause của player
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

    /// <summary>
    /// Đọc thông báo "Bạn đã vào trong phạm vi quán..." trước khi thuyết minh.
    /// Được gọi bởi OnAnnounceEnterShop của WalkingSimulationService.
    /// </summary>
    public async Task AnnounceEnterShop(ShopModel shop)
    {
        try
        {
            // Hủy bất kỳ TTS nào đang chạy
            _ttsCts.Cancel();
            _ttsCts = new CancellationTokenSource();

            var locales = await TextToSpeech.Default.GetLocalesAsync();
            var vnLocale = locales.FirstOrDefault(l => l.Language == "vi" && l.Country == "VN")
                        ?? locales.FirstOrDefault(l => l.Language == "vi");

            var options = new SpeechOptions
            {
                Locale = vnLocale,
                Pitch = 0.9f,
                Volume = 1.0f
            };

            string announcement = $"Bạn đã vào trong phạm vi quán {shop.Name}.";
            await TextToSpeech.Default.SpeakAsync(announcement, options, _ttsCts.Token);
        }
        catch { }
    }

    /// <summary>
    /// Bắt đầu thuyết minh shop. Gọi sau AnnounceEnterShop.
    /// </summary>
    public async Task OnEnterShop(ShopModel shop)
    {
        if (_currentShop == shop && IsPlayerVisible) return;

        _currentShop = shop;

        IsPlayerVisible = true;
        CurrentShopName = shop.Name;
        CurrentShopImage = shop.ImageUrl ?? "";
        PlayerStatus = "Đang thuyết minh...";
        PlayIcon = "⏸";

        await StartReading();
    }

    /// <summary>
    /// Dừng thuyết minh khi người dùng ra khỏi bán kính shop.
    /// Được gọi bởi OnExitShop của WalkingSimulationService.
    /// </summary>
    public void OnExitShop()
    {
        try
        {
            _ttsCts.Cancel();
            _ttsCts = new CancellationTokenSource();

            _playCts.Cancel();
            _playCts = new CancellationTokenSource();

            IsPlayerVisible = false;
            PlayerStatus = "";
            PlayIcon = "▶";
            _currentShop = null;
        }
        catch { }
    }

    [RelayCommand]
    async Task PlayPause()
    {
        if (!_playCts.IsCancellationRequested)
        {
            // Đang chạy → dừng lại
            _ttsCts.Cancel();
            _playCts.Cancel();
            PlayIcon = "▶";
            PlayerStatus = "Đã tạm dừng";
        }
        else
        {
            // Đang dừng → phát lại
            _playCts = new CancellationTokenSource();
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
        // Tạo token mới cho lần đọc này, độc lập với announce
        _ttsCts.Cancel();
        _ttsCts = new CancellationTokenSource();

        // Link với _playCts để PlayPause cũng có thể dừng được
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            _ttsCts.Token, _playCts.Token);

        try
        {
            var options = await BuildSpeechOptions();

            string name = _currentShop?.Name ?? "";
            string desc = _currentShop?.Description ?? "Mời bạn ghé thăm.";
            string content = $"{name}. {desc}"
                .Replace("\n", ". ")
                .Replace("  ", " ");

            await TextToSpeech.Default.SpeakAsync(content, options, linked.Token);

            PlayIcon = "▶";
            PlayerStatus = "Đã kết thúc";
        }
        catch
        {
            // Bị cancel (dừng tay hoặc ra khỏi shop) — không đổi icon ở đây
            // để OnExitShop / PlayPause tự xử lý UI
        }
    }

    /// <summary>
    /// Tạo SpeechOptions với locale tiếng Việt, dùng chung cho announce và StartReading.
    /// </summary>
    private async Task<SpeechOptions> BuildSpeechOptions()
    {
        var locales = await TextToSpeech.Default.GetLocalesAsync();
        var vnLocale = locales.FirstOrDefault(l => l.Language == "vi" && l.Country == "VN")
                    ?? locales.FirstOrDefault(l => l.Language == "vi");

        return new SpeechOptions
        {
            Locale = vnLocale,
            Pitch = 0.9f,
            Volume = 1.0f
        };
    }

    // 👇 3. LOGIC LOAD DATABASE
    [RelayCommand]
    public async Task LoadData()
    {
        var data = await _dbService.GetShopsAsync();
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Shops.Clear();
            foreach (var item in data)
            {
                Shops.Add(item);
            }
        });
    }
}