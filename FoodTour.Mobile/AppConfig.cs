namespace FoodTour.Mobile
{
    public static class AppConfig
    {
        // Chuyển đổi giữa môi trường Local và Production
        public static bool IsLocalEnvironment = false;

        public static string ApiBaseUrl
        {
            get
            {
                if (IsLocalEnvironment)
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
