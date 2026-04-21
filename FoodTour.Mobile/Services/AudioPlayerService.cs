using Android.OS;
using FoodTour.Mobile.Models;
using FoodTour.Mobile.Helpers;
using FoodTour.Mobile.Messages;
using Plugin.Maui.Audio;
using CommunityToolkit.Mvvm.Messaging;
using AndroidAudioManager = Android.Media.AudioManager;
using AndroidAudioFocus = Android.Media.AudioFocus;
using AndroidAudioAttributes = Android.Media.AudioAttributes;
using AndroidAudioUsageKind = Android.Media.AudioUsageKind;
using AndroidAudioContentType = Android.Media.AudioContentType;
using AndroidAudioFocusRequestClass = Android.Media.AudioFocusRequestClass;
using AndroidBuildVersionCodes = Android.OS.BuildVersionCodes;

namespace FoodTour.Mobile.Services;

public class AudioPlayerService : IAudioPlayerService, IRecipient<AudioFilesUpdatedMessage>, IDisposable
{
    private readonly ILocalizationService _localizationService;
    private ShopModel? _currentShop;
    private bool _isPlayerVisible;
    private bool _isPlaying;
    private string _playerStatus = "";

    private IAudioPlayer? _player;
    private IDispatcherTimer? _progressTimer;

    private readonly AndroidAudioFocusListener _audioFocusListener;
    private readonly AndroidAudioManager? _androidAudioManager;
    private AndroidAudioFocusRequestClass? _audioFocusRequest;
    private bool _wasInterrupted = false;

    public AudioPlayerService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
        // Khởi tạo Android Audio Focus trong cùng constructor
        _audioFocusListener = new AndroidAudioFocusListener(this);
        var context = Android.App.Application.Context;
        _androidAudioManager = (AndroidAudioManager?)context.GetSystemService(Android.Content.Context.AudioService);

        // Đăng ký nhận sự kiện khi file audio mới được tải về disk (từ DownloadUpdateAsync)
        // Đây là điểm xử lý được nhất: trực tiếp tại service nắm giữ player,
        // không phụ thuộc vào geofencing hay UI layer.
        WeakReferenceMessenger.Default.Register<AudioFilesUpdatedMessage>(this);
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

    public event Action? PlaybackEnded;
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
            // file local không tồn tại => tải từ cloud server và lưu vào storage
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
            RequestAudioFocus();

            //dùng thư viện Plungin.MAUI.Audio để phát audio
            _player.Play();

            //Cập nhật biến trạng thái (Status, IsPlaying)
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

    public async Task PlayAsync(string audioUrl)
    {
        if (string.IsNullOrEmpty(audioUrl)) return;

        IsPlayerVisible = true;
        _playerStatus = "Đang chuẩn bị audio...";
        _isPlaying = true;
        StateChanged?.Invoke();

        try
        {
            _player?.Dispose();
            string resolvedPath = ImagePathHelper.ResolveImageUrl(audioUrl);
            
            if (resolvedPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                _playerStatus = "Đang tải audio...";
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
            RequestAudioFocus();

            _player.Play();

            _playerStatus = "Đang phát audio...";
            _isPlaying = true;
            StartTimer();
            StateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi phát Audio: {ex.Message}");
            _playerStatus = "Lỗi phát audio";
            _isPlaying = false;
            StateChanged?.Invoke();
        }
    }

    public async Task PlayPauseAsync()
    {
        if (_player == null) return;

        if (_isPlaying)
        {
            _wasInterrupted = false; // User chủ động dừng -> không auto-resume
            _player.Pause();
            _isPlaying = false;
            _playerStatus = _localizationService["Audio_Paused"] ?? "Đã tạm dừng";
        }
        else
        {
            _wasInterrupted = false; // User chủ động resume
            RequestAudioFocus(); // -> request lại focus rồi phát
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
            _wasInterrupted = false;
            AbandonAudioFocus();

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
            _wasInterrupted = false;
            AbandonAudioFocus();
            StateChanged?.Invoke();
            PlaybackEnded?.Invoke();
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

    private void StopTimer() => _progressTimer?.Stop();

    public void Dispose()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        Stop();
    }

    // ─────────────────────────────────────────
    // Hot-reload audio khi file .mp3 mới được ghi xuống disk
    // ─────────────────────────────────────────
    public async void Receive(AudioFilesUpdatedMessage message)
    {
        // Chỉ xử lý nếu đang phát một shop và shop đó có trong danh sách được cập nhật
        if (_currentShop == null) return;
        if (!message.Value.Contains(_currentShop.Id)) return;

        System.Diagnostics.Debug.WriteLine($"[AudioPlayer] File audio mới cho shop '{_currentShop.Name}' đã sẵn sàng, reload...");

        // Lưu lại tập tham chiếu shop trước khi Stop() xóa _currentShop
        var shopToReload = _currentShop;

        // Dừng player cũ hoàn toàn (giải phóng FileDescriptor/stream cũ)
        // Sau Stop(): _currentShop = null, _isPlaying = false, _player = null
        Stop();

        // Phát lại từ đầu với file mới nhất trên disk
        // PlayShopAsync sẽ gọi File.OpenRead() mới → đọc nội dung mới
        // (không bị early-return vì _currentShop đã bị null)
        await PlayShopAsync(shopToReload);
    }

    // ANDROID AUDIO FOCUS
    private void RequestAudioFocus()
    {
        if (_androidAudioManager == null) return;

        if (Build.VERSION.SdkInt >= AndroidBuildVersionCodes.O)
        {
#pragma warning disable CA1416
            var audioAttributes = new AndroidAudioAttributes.Builder()
                .SetUsage(AndroidAudioUsageKind.AssistanceNavigationGuidance)!
                .SetContentType(AndroidAudioContentType.Speech)!
                .Build();

            if (audioAttributes == null) return;

            var request = new AndroidAudioFocusRequestClass.Builder(AndroidAudioFocus.Gain)
                .SetAudioAttributes(audioAttributes)!
                .SetAcceptsDelayedFocusGain(true)!
                .SetWillPauseWhenDucked(true)!
                .SetOnAudioFocusChangeListener(_audioFocusListener)!
                .Build();

            if (request == null) return;

            _audioFocusRequest = request;
            _androidAudioManager.RequestAudioFocus(_audioFocusRequest);
#pragma warning restore CA1416
        }
        else
        {
#pragma warning disable CA1422
            _androidAudioManager.RequestAudioFocus(
                _audioFocusListener,
                Android.Media.Stream.Music,
                AndroidAudioFocus.Gain);
#pragma warning restore CA1422
        }
    }

    private void AbandonAudioFocus()
    {
        if (_androidAudioManager == null) return;
        try
        {
            if (Build.VERSION.SdkInt >= AndroidBuildVersionCodes.O)
            {
#pragma warning disable CA1416
                var request = _audioFocusRequest;
                if (request != null)
                {
                    _androidAudioManager.AbandonAudioFocusRequest(request);
                    _audioFocusRequest = null;
                }
#pragma warning restore CA1416
            }
            else
            {
#pragma warning disable CA1422
                _androidAudioManager.AbandonAudioFocus(_audioFocusListener);
#pragma warning restore CA1422
            }
        }
        catch { }
    }

    private void HandleAudioFocusChange(AndroidAudioFocus focusChange)
    {
        // Dispatch về main thread để tránh race condition với UI và player
        Application.Current?.Dispatcher.Dispatch(() =>
        {
            switch (focusChange)
            {
                // Focus được trả lại
                case AndroidAudioFocus.Gain:
                    // Chỉ tự động phát tiếp nếu trước đó bị hệ thống ngắt tạm thời
                    if (_wasInterrupted && !_isPlaying && _player != null && _isPlayerVisible)
                    {
                        _wasInterrupted = false;
                        _player.Play();
                        _isPlaying = true;
                        _playerStatus = _localizationService["Audio_Playing"] ?? "Đang phát audio...";
                        StartTimer();
                        StateChanged?.Invoke();
                    }
                    break;

                // Mất focus tạm thời (thông báo, cuộc gọi) → Tạm dừng và TỰ ĐỘNG PHÁT TIẾP khi âm thanh kia kết thúc
                case AndroidAudioFocus.LossTransient:
                case AndroidAudioFocus.LossTransientCanDuck:
                    if (_isPlaying && _player != null)
                    {
                        _wasInterrupted = true;
                        _player.Pause();
                        _isPlaying = false;
                        _playerStatus = _localizationService["Audio_Paused"] ?? "Đã tạm dừng";
                        StopTimer();
                        StateChanged?.Invoke();
                    }
                    break;

                // Mất focus vĩnh viễn (mở app nhạc, YouTube…) → Dừng và KHÔNG TỰ PHÁT LẠI
                case AndroidAudioFocus.Loss:
                    if (_isPlaying && _player != null)
                    {
                        _wasInterrupted = false;
                        _player.Pause();
                        _isPlaying = false;
                        _playerStatus = _localizationService["Audio_Paused"] ?? "Đã tạm dừng";
                        StopTimer();
                        AbandonAudioFocus();
                        StateChanged?.Invoke();
                    }
                    break;
            }
        });
    }

    private sealed class AndroidAudioFocusListener
        : Java.Lang.Object, AndroidAudioManager.IOnAudioFocusChangeListener
    {
        private readonly AudioPlayerService _service;

        public AndroidAudioFocusListener(AudioPlayerService service) => _service = service;

        public void OnAudioFocusChange([Android.Runtime.GeneratedEnum] AndroidAudioFocus focusChange) => _service.HandleAudioFocusChange(focusChange);
    }
}