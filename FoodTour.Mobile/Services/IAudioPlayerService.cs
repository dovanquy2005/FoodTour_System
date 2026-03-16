using FoodTour.Mobile.Models;

namespace FoodTour.Mobile.Services;

public interface IAudioPlayerService
{
    bool IsPlaying { get; }
    bool IsPlayerVisible { get; set; }
    ShopModel? CurrentShop { get; }
    string PlayerStatus { get; }
    
    Task PlayShopAsync(ShopModel shop);
    Task PlayPauseAsync();
    void Stop();
    
    event Action StateChanged;
}
