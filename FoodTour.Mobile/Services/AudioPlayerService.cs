using FoodTour.Mobile.Models;
using FoodTour.Mobile.Helpers;
using Plugin.Maui.Audio;

namespace FoodTour.Mobile.Services;

public class AudioPlayerService : IAudioPlayerService, IDisposable
{
    private readonly ILocalizationService _localizationService;
    private ShopModel? _currentShop;
    private bool _isPlayerVisible;
    private bool _isPlaying;
    private string _playerStatus = "";

    private IAudioPlayer? _player;
    private IDispatcherTimer? _progressTimer;

    public AudioPlayerService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public bool IsPlaying => _isPlaying;
    public ShopModel? CurrentShop => _currentShop;
    public string PlayerStatus => _playerStatus;
    
    public double CurrentPosition => _player?.CurrentPosition ?? 0;
    public double Duration => _player?.Duration ?? 1;

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
        
        string? audioUrlOrPath = shop.AudioUrl;
        if (string.IsNullOrEmpty(audioUrlOrPath))
        {
            _playerStatus = _localizationService["Audio_NoExplanation"] ?? "Không có thuyết minh";
            _isPlaying = false;
            StateChanged?.Invoke();
            return;
        }

        _playerStatus = _localizationService["Audio_Preparing"] ?? "Đang chuẩn bị audio...";
        _isPlaying = true;
        StateChanged?.Invoke();

        try
        {
            _player?.Dispose();
            
            // Lấy Audio file: ưu tiên cache cục bộ, nếu không thì tải từ mạng
            string resolvedPath = ImagePathHelper.ResolveImageUrl(audioUrlOrPath);
            
            if (resolvedPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                _playerStatus = _localizationService["Audio_Downloading"] ?? "Đang tải audio...";
                StateChanged?.Invoke();
                
                using var httpClient = new HttpClient();
                var bytes = await httpClient.GetByteArrayAsync(resolvedPath);
                
                string fileName = Path.GetFileName(new Uri(resolvedPath).LocalPath);
                resolvedPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                await File.WriteAllBytesAsync(resolvedPath, bytes);
            }

            var audioStream = File.OpenRead(resolvedPath);
            _player = AudioManager.Current.CreatePlayer(audioStream);
            _player.PlaybackEnded += OnPlaybackEnded;
            _player.Play();
            
            _playerStatus = _localizationService["Audio_Playing"] ?? "Đang phát audio...";
            _isPlaying = true;
            StartTimer();
            StateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi phát Audio: {ex.Message}");
            _playerStatus = _localizationService["Audio_Error"] ?? "Lỗi phát audio";
            _isPlaying = false;
            StateChanged?.Invoke();
        }
    }

    public async Task PlayPauseAsync()
    {
        if (_player == null) return;

        if (_isPlaying)
        {
            _player.Pause();
            _isPlaying = false;
            _playerStatus = _localizationService["Audio_Paused"] ?? "Đã tạm dừng";
        }
        else
        {
            _player.Play();
            _isPlaying = true;
            _playerStatus = _localizationService["Audio_Playing"] ?? "Đang phát audio...";
        }
        
        StateChanged?.Invoke();
        await Task.CompletedTask;
    }

    public void Stop()
    {
        try
        {
            StopTimer();

            if (_player != null)
            {
                _player.PlaybackEnded -= OnPlaybackEnded;
                _player.Stop();
                _player.Dispose();
                _player = null;
            }

            _isPlayerVisible = false;
            _isPlaying = false;
            _playerStatus = "";
            _currentShop = null;
            StateChanged?.Invoke();
        }
        catch { }
    }

    public void Seek(double positionInSeconds)
    {
        if (_player != null && positionInSeconds >= 0 && positionInSeconds <= _player.Duration)
        {
            _player.Seek(positionInSeconds);
            StateChanged?.Invoke();
        }
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _isPlaying = false;
            _playerStatus = _localizationService["Audio_Ended"] ?? "Đã kết thúc";
            StopTimer();
            StateChanged?.Invoke();
        });
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

    public void Dispose()
    {
        Stop();
    }
}
