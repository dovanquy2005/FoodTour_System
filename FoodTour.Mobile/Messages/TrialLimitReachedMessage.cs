using CommunityToolkit.Mvvm.Messaging.Messages;

namespace FoodTour.Mobile.Messages
{
    /// <summary>
    /// Message gửi khi người dùng đã hết lượt quét QR chủ động (AppScan).
    /// UI (AppShell/MapPage) lắng nghe để hiển thị thông báo nâng cấp Premium.
    /// </summary>
    public class TrialLimitReachedMessage : ValueChangedMessage<string>
    {
        public TrialLimitReachedMessage(string message) : base(message) { }
    }
}
