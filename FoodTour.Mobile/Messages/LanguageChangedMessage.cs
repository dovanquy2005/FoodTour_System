using CommunityToolkit.Mvvm.Messaging.Messages;

namespace FoodTour.Mobile.Messages
{
    /// <summary>
    /// Message phát đi toàn cục khi người dùng đổi ngôn ngữ trong cài đặt (Settings).
    /// Các Service (như WalkingSimulationService) sẽ Subscribe thông điệp này để 
    /// phản hồi tức thì (Reactive) đổi ngay Audio mà không cần reset vòng đời ứng dụng.
    /// </summary>
    public class LanguageChangedMessage : ValueChangedMessage<string>
    {
        public LanguageChangedMessage(string newLanguageCode) : base(newLanguageCode)
        {
        }
    }
}
