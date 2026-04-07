using CommunityToolkit.Mvvm.Messaging.Messages;

namespace FoodTour.Mobile.Messages
{
    /// <summary>
    /// Message gửi khi phát hiện có bản cập nhật mới trên 4G/LTE.
    /// AppShell lắng nghe để hiển thị Badge đỏ trên tab Alerts.
    /// </summary>
    public class NewUpdateAvailableMessage : ValueChangedMessage<int>
    {
        /// <summary>
        /// Dung lượng ước tính (bytes) của bản cập nhật.
        /// </summary>
        public long EstimatedSize { get; }

        public NewUpdateAvailableMessage(int shopCount, long estimatedSize) : base(shopCount)
        {
            EstimatedSize = estimatedSize;
        }
    }
}
