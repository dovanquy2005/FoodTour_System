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

    // Hỗ trợ thanh tiến trình và pause/resume
    private IDispatcherTimer? _progressTimer;
    private List<string> _sentences = new();
    private int _currentSentenceIndex = 0;
    private double _estimatedDuration = 0;
    private double _currentPosition = 0;
    private const double CharsPerSecond = 14.0; // Tốc độ đọc ước tính (ký tự/giây)

    public bool IsPlaying => _isPlaying;
    public ShopModel? CurrentShop => _currentShop;
    public string PlayerStatus => _playerStatus;
    
    public double CurrentPosition => _currentPosition;
    public double Duration => _estimatedDuration;

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
        
        PrepareContent(shop);
        
        StateChanged?.Invoke();

        await StartReading();
    }

    private void PrepareContent(ShopModel shop)
    {
        string name = shop.Name ?? "";
        string desc = shop.Description ?? "Mời bạn ghé thăm.";
        string fullContent = $"{name}. {desc}".Replace("\n", ". ").Replace("  ", " ");
        
        // Tách thành các đoạn câu nhỏ để có thể dừng/tiếp tục chính xác hơn
        _sentences = fullContent.Split(new[] { ". ", ".", "?", "!" }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim() + ".")
                                .Where(s => s.Length > 1)
                                .ToList();
                                
        _currentSentenceIndex = 0;
        _currentPosition = 0;
        
        // Ước tính tổng thời lượng dựa trên số ký tự
        int totalChars = _sentences.Sum(s => s.Length);
        _estimatedDuration = totalChars / CharsPerSecond;
        if (_estimatedDuration < 1) _estimatedDuration = 1;
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
            
            StopTimer();

            _isPlayerVisible = false;
            _isPlaying = false;
            _playerStatus = "";
            _currentShop = null;
            _currentPosition = 0;
            _currentSentenceIndex = 0;
            StateChanged?.Invoke();
        }
        catch { }
    }

    public void Seek(double positionInSeconds)
    {
        if (_sentences.Count == 0) return;
        
        positionInSeconds = Math.Clamp(positionInSeconds, 0, _estimatedDuration);
        _currentPosition = positionInSeconds;
        
        // Cập nhật lại câu đang đọc dựa trên thời gian
        double targetCharCount = positionInSeconds * CharsPerSecond;
        int accumulated = 0;
        
        for (int i = 0; i < _sentences.Count; i++)
        {
            accumulated += _sentences[i].Length;
            if (accumulated >= targetCharCount)
            {
                _currentSentenceIndex = i;
                break;
            }
        }
        
        // Nếu đang phát thì phải huỷ dòng text hiện tại để đọc từ câu mới
        if (_isPlaying)
        {
            _ttsCts.Cancel();
            _ttsCts = new CancellationTokenSource();
            _ = StartReading();
        }
        
        StateChanged?.Invoke();
    }

    private async Task StartReading()
    {
        _ttsCts.Cancel();
        _ttsCts = new CancellationTokenSource();

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_ttsCts.Token, _playCts.Token);
        StartTimer();

        try
        {
            var options = await BuildSpeechOptions();

            // Lặp qua các câu từ vị trí lưu trữ gần nhất
            while (_currentSentenceIndex < _sentences.Count)
            {
                linked.Token.ThrowIfCancellationRequested();
                
                string sentenceToSpeak = _sentences[_currentSentenceIndex];
                await TextToSpeech.Default.SpeakAsync(sentenceToSpeak, options, linked.Token);
                
                _currentSentenceIndex++;
            }

            // Hoàn thành hết văn bản
            _isPlaying = false;
            _playerStatus = "Đã kết thúc";
            _currentPosition = _estimatedDuration;
            StopTimer();
            StateChanged?.Invoke();
        }
        catch (OperationCanceledException)
        {
            // Bị dừng giữa chừng (User ấn pause hoặc stop)
            StopTimer();
        }
        catch
        {
            StopTimer();
        }
    }
    
    private void StartTimer()
    {
        if (_progressTimer == null)
        {
            _progressTimer = Application.Current?.Dispatcher.CreateTimer();
            if (_progressTimer != null)
            {
                _progressTimer.Interval = TimeSpan.FromMilliseconds(500);
                _progressTimer.Tick += (s, e) =>
                {
                    if (_isPlaying)
                    {
                        _currentPosition += 0.5;
                        if (_currentPosition > _estimatedDuration)
                            _currentPosition = _estimatedDuration;
                        StateChanged?.Invoke();
                    }
                };
            }
        }
        _progressTimer?.Start();
    }

    private void StopTimer()
    {
        _progressTimer?.Stop();
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
