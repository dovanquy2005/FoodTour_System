using Microsoft.Maui.Networking;

namespace FoodTour.Mobile.Helpers
{
    public static class NetworkHelper
    {
        /// <summary>
        /// Phân loại kết nối mạng thành 2 nhóm:
        /// - Mạng không tốn phí (WiFi / Ethernet) → trả về true, cho phép auto-sync ngầm.
        /// - Mạng di động (3G/4G/5G/Cellular) → trả về false, phải hỏi người dùng trước.
        ///
        /// Ưu tiên Cellular: nếu Cellular có mặt mà WiFi KHÔNG có mặt → luôn trả false.
        /// Lý do: Android đôi khi báo cả WiFi lẫn Cellular khi đang chuyển vùng phủ sóng,
        /// gây nhầm lẫn người dùng 4G thành WiFi.
        /// </summary>
        public static bool IsFreeNetwork()
        {
            var currentAccess = Connectivity.Current.NetworkAccess;

            // Không có mạng → false
            if (currentAccess != NetworkAccess.Internet)
                return false;

            var profiles = Connectivity.Current.ConnectionProfiles;

            bool hasWiFi      = profiles.Contains(ConnectionProfile.WiFi);
            bool hasEthernet  = profiles.Contains(ConnectionProfile.Ethernet);
            bool hasCellular  = profiles.Contains(ConnectionProfile.Cellular);

            // Nếu chỉ có Cellular (không có WiFi/Ethernet) → mạng di động, trả false
            if (hasCellular && !hasWiFi && !hasEthernet)
                return false;

            // Có WiFi hoặc Ethernet → mạng miễn phí
            if (hasWiFi || hasEthernet)
                return true;

            // Không rõ loại (Bluetooth, Unknown,...) → coi là không miễn phí để an toàn
            return false;
        }
    }
}
