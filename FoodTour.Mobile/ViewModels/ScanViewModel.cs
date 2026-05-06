using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoodTour.Mobile.Services;
using FoodTour.Mobile.Models;
using FoodTour.Mobile.Views;
using System.Text.RegularExpressions;

namespace FoodTour.Mobile.ViewModels;

public partial class ScanViewModel : BaseViewModel, IDisposable
{
    private readonly WalkingSimulationService _walkingService;
    private readonly DatabaseService _dbService;
    private readonly IAudioPlayerService _audioService;
    private readonly LogService _logService;
    private IDispatcherTimer? _positionTimer;

    [ObservableProperty]
    private bool isScanning = true;

    [ObservableProperty]
    private bool isSuccess;

    [ObservableProperty]
    private bool isLoadingShops;

    [ObservableProperty]
    private ObservableCollection<ShopModel> shops = new();

    [ObservableProperty]
    private string? playingShopId;

    [ObservableProperty]
    private bool isPlaying;

    [ObservableProperty]
    private double currentPosition;

    [ObservableProperty]
    private double duration;

    public event Action? PlaybackEnded;

    private static readonly Regex GuidRegex = new(
        @"([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
        RegexOptions.Compiled);

    private static readonly Regex GlobalQrRegex = new(
        @"^(https?://[^/]+)?/?foodtour/?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private const int TriggerTypeAppScan = 1;

    public ScanViewModel(WalkingSimulationService walkingService, DatabaseService dbService, IAudioPlayerService audioService, LogService logService)
    {
        _walkingService = walkingService;
        _dbService = dbService;
        _audioService = audioService;
        _logService = logService;

        _audioService.PlaybackEnded += OnPlaybackEnded;
        StartPositionTimer();
    }

    private void StartPositionTimer()
    {
        _positionTimer = Application.Current?.Dispatcher.CreateTimer();
        if (_positionTimer != null)
        {
            _positionTimer.Interval = TimeSpan.FromMilliseconds(500);
            _positionTimer.Tick += (s, e) =>
            {
                if (_audioService.IsPlaying)
                {
                    CurrentPosition = _audioService.CurrentPosition;
                    Duration = _audioService.Duration;
                }
            };
            _positionTimer.Start();
        }
    }

    private void OnPlaybackEnded()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            PlayingShopId = null;
            IsPlaying = false;
            CurrentPosition = 0;
            Duration = 0;
            PlaybackEnded?.Invoke();
        });
    }

    public void UpdatePlayingState(string? shopId)
    {
        if (PlayingShopId != shopId)
        {
            PlayingShopId = shopId;
            IsPlaying = _audioService.IsPlaying;
            CurrentPosition = _audioService.CurrentPosition;
            Duration = _audioService.Duration;
        }
        else
        {
            IsPlaying = _audioService.IsPlaying;
            CurrentPosition = _audioService.CurrentPosition;
            Duration = _audioService.Duration;
        }
    }

    [RelayCommand]
    public async Task LoadShopsAsync()
    {
        IsLoadingShops = true;
        try
        {
            var data = await _dbService.GetShopsAsync();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Shops = new ObservableCollection<ShopModel>(data);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ScanViewModel] LoadShops error: {ex.Message}");
        }
        finally
        {
            IsLoadingShops = false;
        }
    }

    [RelayCommand]
    public async Task ProcessQrCodeAsync(string qrContent)
    {
        if (!IsScanning) return;

        try
        {
            IsScanning = false;

            if (IsGlobalQr(qrContent))
            {
                System.Diagnostics.Debug.WriteLine($"[ScanViewModel] Global QR detected: {qrContent}");
                await LoadShopsAsync();
                IsSuccess = true;
                return;
            }

            string? shopId = ExtractShopId(qrContent);

            if (string.IsNullOrEmpty(shopId))
            {
                // Invalid QR, resume scanning
                IsScanning = true;
                return;
            }

            var shop = await _dbService.GetShopAsync(shopId);

            if (shop == null)
            {
                // Shop not found, resume scanning
                IsScanning = true;
                return;
            }

            // Valid specific shop
            bool shouldPlay = await CheckTrialAsync(shop.Id);
            
            // Still switch to success view to show the list, but maybe we could pre-filter to only this shop or play immediately.
            // For now, load all shops and play the scanned one if allowed.
            await LoadShopsAsync();
            IsSuccess = true;

            if (shouldPlay)
            {
                await PlayShopAudioAsync(shop);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ScanViewModel] Lỗi xử lý QR: {ex.Message}");
            IsScanning = true; // Resume on error
        }
    }

    private async Task<bool> CheckTrialAsync(string shopId)
    {
        var deviceId = App.DeviceId;
        if (!string.IsNullOrEmpty(deviceId))
        {
            try
            {
                var trialResult = await _dbService.RecordTrialAsync(deviceId, shopId, TriggerTypeAppScan);
                if (trialResult != null && !trialResult.Allowed)
                {
                    var trialAlertMessage = "Bạn đã hết 3 lượt quét mã chủ động trong 24h.\n\n" +
                        "💡 Hãy nâng cấp Premium để nghe không giới hạn, " +
                        "hoặc sử dụng tính năng tự động thuyết minh trên tab Bản đồ (miễn phí).";
                    
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
                        {
                            await Application.Current.Windows[0].Page!.DisplayAlert("Hết lượt nghe thử", trialAlertMessage, "Đã hiểu");
                        }
                    });
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ScanViewModel] Trial check error: {ex.Message}");
            }
        }
        return true;
    }

    [RelayCommand]
    async Task PlayShopAudioAsync(ShopModel shop)
    {
        if (shop == null) return;

        try
        {
            UpdatePlayingState(shop.Id);
            await _walkingService.PlayShopFromQrAsync(shop);
            
            // Log Trail asynchronously
            await _logService.LogTrailAsync(App.DeviceId ?? "unknown", shop.Id, "listen_audio");
            
            System.Diagnostics.Debug.WriteLine($"[ScanViewModel] Playing audio for: {shop.Name}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ScanViewModel] PlayShopAudio error: {ex.Message}");
        }
    }

    [RelayCommand]
    async Task TogglePlayPauseAsync()
    {
        try
        {
            await _audioService.PlayPauseAsync();
            IsPlaying = _audioService.IsPlaying;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ScanViewModel] TogglePlayPause error: {ex.Message}");
        }
    }

    [RelayCommand]
    void SeekAudio(double position)
    {
        try
        {
            _audioService.Seek(position);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ScanViewModel] SeekAudio error: {ex.Message}");
        }
    }

    [RelayCommand]
    void ResetScanner()
    {
        IsSuccess = false;
        IsScanning = true;
    }

    private static bool IsGlobalQr(string qrContent)
    {
        if (string.IsNullOrWhiteSpace(qrContent)) return false;
        return GlobalQrRegex.IsMatch(qrContent.Trim());
    }

    private static string? ExtractShopId(string qrContent)
    {
        if (string.IsNullOrWhiteSpace(qrContent)) return null;
        var match = GuidRegex.Match(qrContent);
        return match.Success ? match.Groups[1].Value : null;
    }

    public void Dispose()
    {
        _positionTimer?.Stop();
        if (_audioService != null)
        {
            _audioService.PlaybackEnded -= OnPlaybackEnded;
        }
    }
}