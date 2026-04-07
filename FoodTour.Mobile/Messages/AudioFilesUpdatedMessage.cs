using CommunityToolkit.Mvvm.Messaging.Messages;

namespace FoodTour.Mobile.Messages
{
    /// <summary>
    /// Message phát đi sau khi DownloadUpdateAsync tải xong file audio mới về disk.
    /// WalkingSimulationService lắng nghe để reload audio đang phát ngay lập tức
    /// mà không cần khởi động lại ứng dụng.
    /// Value: danh sách shopId (JSON array string) vừa được cập nhật audio.
    /// </summary>
    public class AudioFilesUpdatedMessage : ValueChangedMessage<List<string>>
    {
        public AudioFilesUpdatedMessage(List<string> updatedShopIds) : base(updatedShopIds)
        {
        }
    }
}
