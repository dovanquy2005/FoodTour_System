using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoodTour.Mobile.Models;
using FoodTour.Mobile.Services;

namespace FoodTour.Mobile.ViewModels
{
    public partial class GlobalShopListViewModel : BaseViewModel
    {
        private readonly DatabaseService _dbService;
        private readonly WalkingSimulationService _walkingService;
        private readonly IAudioPlayerService _audioService;
        private List<ShopModel> _allShops = new();
        private IDispatcherTimer? _positionTimer;

        [ObservableProperty]
        ObservableCollection<ShopModel> shops = new();

        [ObservableProperty]
        private bool isLoading = true;

        [ObservableProperty]
        private bool isOffline;

        [ObservableProperty]
        private bool hasShops;

        [ObservableProperty]
        private string? playingShopId;

        [ObservableProperty]
        private bool isPlaying;

        [ObservableProperty]
        private double currentPosition;

        [ObservableProperty]
        private double duration;

        public event Action? PlaybackEnded;

        public GlobalShopListViewModel(DatabaseService dbService, WalkingSimulationService walkingService, IAudioPlayerService audioService)
        {
            _dbService = dbService;
            _walkingService = walkingService;
            _audioService = audioService;
            IsOffline = Preferences.Default.Get("IsOfflineMode", false);

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
        public async Task LoadShops()
        {
            IsLoading = true;
            try
            {
                var data = await _dbService.GetShopsAsync();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _allShops = data;
                    Shops = new ObservableCollection<ShopModel>(_allShops);
                    HasShops = Shops.Count > 0;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GlobalShopListViewModel] LoadShops error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        async Task PlayShopAudio(ShopModel shop)
        {
            if (shop == null) return;

            try
            {
                UpdatePlayingState(shop.Id);
                await _walkingService.PlayShopFromQrAsync(shop);
                System.Diagnostics.Debug.WriteLine($"[GlobalShopListViewModel] Playing audio for: {shop.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GlobalShopListViewModel] PlayShopAudio error: {ex.Message}");
            }
        }

        [RelayCommand]
        async Task TogglePlayPause()
        {
            try
            {
                await _audioService.PlayPauseAsync();
                IsPlaying = _audioService.IsPlaying;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GlobalShopListViewModel] TogglePlayPause error: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"[GlobalShopListViewModel] SeekAudio error: {ex.Message}");
            }
        }

        [RelayCommand]
        async Task NavigateToShopOnMap(ShopModel shop)
        {
            if (shop == null) return;

            await Shell.Current.GoToAsync("//MainTabs/MapPage");
        }
    }
}