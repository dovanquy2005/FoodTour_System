using FoodTour.Mobile.Models;

namespace FoodTour.Mobile.Services;

public interface IAudioPlayerService
{
    bool IsPlaying { get; }
    bool IsPlayerVisible { get; set; }
    ShopModel? CurrentShop { get; }
    string PlayerStatus { get; }
    
    // Thuộc tính phục vụ thanh tiến trình
    double CurrentPosition { get; }
    double Duration { get; }
    
    Task PlayShopAsync(ShopModel shop);
    Task PlayPauseAsync();
    void Stop();
    void Seek(double positionInSeconds);
    
    event Action StateChanged;
}
