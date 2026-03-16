using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoodTour.Mobile.Services;
using FoodTour.Mobile.Models;

namespace FoodTour.Mobile.ViewModels;

public partial class PlayerViewModel : ObservableObject
{
    private readonly IAudioPlayerService _audioService;

    [ObservableProperty] private bool isMinimized;
    [ObservableProperty] private double playerTranslationX;
    [ObservableProperty] private double playerOpacity = 1;
    [ObservableProperty] private bool isFullVisible = true;
    
    // Properties proxying the service state
    public bool IsVisible => _audioService.IsPlayerVisible;
    public ShopModel? CurrentShop => _audioService.CurrentShop;
    public string PlayerStatus => _audioService.PlayerStatus;
    public string PlayIcon => _audioService.IsPlaying ? "pause.png" : "play.png";
    public string ShopImage => _audioService.CurrentShop?.ImageUrl ?? "";
    public string ShopName => _audioService.CurrentShop?.Name ?? "Đang tải...";

    public PlayerViewModel(IAudioPlayerService audioService)
    {
        _audioService = audioService;
        _audioService.StateChanged += OnServiceStateChanged;
    }

    private void OnServiceStateChanged()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnPropertyChanged(nameof(IsVisible));
            OnPropertyChanged(nameof(CurrentShop));
            OnPropertyChanged(nameof(PlayerStatus));
            OnPropertyChanged(nameof(PlayIcon));
            OnPropertyChanged(nameof(ShopImage));
            OnPropertyChanged(nameof(ShopName));
            
            // If shop changes, auto-expand if it was minimized? 
            // Better to keep it minimized if the user explicitly did that.
        });
    }

    [RelayCommand]
    private async Task PlayPause()
    {
        await _audioService.PlayPauseAsync();
    }

    [RelayCommand]
    private void Minimize()
    {
        IsMinimized = true;
        // Logic for animation will be handled in code-behind or via triggers if possible, 
        // but here we toggle the state.
    }

    [RelayCommand]
    private void Expand()
    {
        IsMinimized = false;
    }

    [RelayCommand]
    private void SkipPrevious()
    {
        // Placeholder — shop navigation is handled by MapViewModel
    }

    [RelayCommand]
    private void SkipNext()
    {
        // Placeholder — shop navigation is handled by MapViewModel
    }

    [RelayCommand]
    private void Close()
    {
        _audioService.Stop();
    }
}
