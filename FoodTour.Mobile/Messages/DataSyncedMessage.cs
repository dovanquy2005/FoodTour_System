using CommunityToolkit.Mvvm.Messaging.Messages;

namespace FoodTour.Mobile.Messages
{
    /// <summary>
    /// Message gửi khi đồng bộ dữ liệu WiFi hoàn tất.
    /// AppShell lắng nghe để hiển thị Snackbar thông báo.
    /// </summary>
    public class DataSyncedMessage : ValueChangedMessage<string>
    {
        public DataSyncedMessage(string message) : base(message) { }
    }
}
