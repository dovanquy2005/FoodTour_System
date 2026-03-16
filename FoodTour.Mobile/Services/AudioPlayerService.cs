using FoodTour.Mobile.Models;
using Microsoft.Maui.Media;

namespace FoodTour.Mobile.Services;

public class AudioPlayerService : IAudioPlayerService
{
    private CancellationTokenSource _ttsCts = new();
    private CancellationTokenSource _playCts = new();
    private ShopModel? _currentShop;
    private bool _isPlayerVisible;
    private bool _isPlaying;
    private string _playerStatus = "";

    public bool IsPlaying => _isPlaying;
    public ShopModel? CurrentShop => _currentShop;
    public string PlayerStatus => _playerStatus;

    public bool IsPlayerVisible
    {
        get => _isPlayerVisible;
        set
        {
            if (_isPlayerVisible != value)
            {
                _isPlayerVisible = value;
                StateChanged?.Invoke();
            }
        }
    }

    public event Action? StateChanged;

    public async Task PlayShopAsync(ShopModel shop)
    {
        if (_currentShop?.Id == shop.Id && IsPlayerVisible && _isPlaying) return;

        _currentShop = shop;
        IsPlayerVisible = true;
        _isPlaying = true;
        _playerStatus = "Đang thuyết minh...";
        StateChanged?.Invoke();

        await StartReading();
    }

    public async Task PlayPauseAsync()
    {
        if (_isPlaying)
        {
            // Đang chạy → dừng lại
            _ttsCts.Cancel();
            _playCts.Cancel();
            _isPlaying = false;
            _playerStatus = "Đã tạm dừng";
        }
        else
        {
            // Đang dừng → phát lại
            _playCts = new CancellationTokenSource();
            _isPlaying = true;
            _playerStatus = "Đang thuyết minh...";
            await StartReading();
        }
        StateChanged?.Invoke();
    }

    public void Stop()
    {
        try
        {
            _ttsCts.Cancel();
            _ttsCts = new CancellationTokenSource();

            _playCts.Cancel();
            _playCts = new CancellationTokenSource();

            _isPlayerVisible = false;
            _isPlaying = false;
            _playerStatus = "";
            _currentShop = null;
            StateChanged?.Invoke();
        }
        catch { }
    }

    private async Task StartReading()
    {
        _ttsCts.Cancel();
        _ttsCts = new CancellationTokenSource();

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_ttsCts.Token, _playCts.Token);

        try
        {
            var options = await BuildSpeechOptions();

            string name = _currentShop?.Name ?? "";
            string desc = _currentShop?.Description ?? "Mời bạn ghé thăm.";
            string content = $"{name}. {desc}".Replace("\n", ". ").Replace("  ", " ");

            await TextToSpeech.Default.SpeakAsync(content, options, linked.Token);

            _isPlaying = false;
            _playerStatus = "Đã kết thúc";
            StateChanged?.Invoke();
        }
        catch
        {
            // Cancelled
        }
    }

    private async Task<SpeechOptions> BuildSpeechOptions()
    {
        var currentLang = Microsoft.Maui.Storage.Preferences.Default.Get("AppLanguage", "vi"); // Default "vi"
        var locales = await TextToSpeech.Default.GetLocalesAsync();

        // Cố gắng tìm locale khớp với language (vd: "vi", "en") hoặc language đầu tiên của locale (vd: "en-US")
        var selectedLocale = locales.FirstOrDefault(l => l.Language.Equals(currentLang, StringComparison.OrdinalIgnoreCase) 
                                                      || l.Language.StartsWith(currentLang + "-", StringComparison.OrdinalIgnoreCase))
                          ?? locales.FirstOrDefault(l => l.Language.StartsWith("vi", StringComparison.OrdinalIgnoreCase));

        return new SpeechOptions
        {
            Locale = selectedLocale,
            Pitch = 0.9f,
            Volume = 1.0f
        };
    }
}
