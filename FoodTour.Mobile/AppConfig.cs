namespace FoodTour.Mobile
{
    public static class AppConfig
    {
        // Chuyển đổi giữa môi trường Local và Production
        public static bool IsLocalEnvironment = true;
        
        // Cờ đánh dấu tự động fallback sang localhost nếu cloud web server (Render) bị lỗi (như 521)
        public static bool UseLocalFallback = false;

        public static string ApiBaseUrl
        {
            get
            {
                if (IsLocalEnvironment || UseLocalFallback)
                {
                    // Dùng IP của máy host cho Android Emulator
                    return DeviceInfo.Platform == DevicePlatform.Android 
                        ? "http://10.0.2.2:5154" 
                        : "http://localhost:5154";
                }
                
                return "https://foodtour-admin-api.onrender.com";
            }
        }
    }
}
