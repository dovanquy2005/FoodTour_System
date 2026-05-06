using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FoodTour.Mobile.Models;

namespace FoodTour.Mobile.Services
{
    public class LogService
    {
        private readonly HttpClient _httpClient;

        public LogService()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        }

        public async Task LogTrailAsync(string deviceId, string shopId, string actionType)
        {
            if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(shopId)) return;

            try
            {
                var payload = new
                {
                    DeviceId = deviceId,
                    ShopId = shopId,
                    ShopItemId = (int?)null,
                    LanguageCode = Microsoft.Maui.Storage.Preferences.Default.Get("AppLanguage", "vi"),
                    Source = "AppManual", // Ghi nhận phát thủ công qua thao tác bấm App
                    BrowserFingerprint = "MAUI_App"
                };

                // Backend sử dụng AudioLogsController để ghi nhận nhật ký nghe
                var url = $"{AppConfig.ApiBaseUrl}/api/AudioLogs/record"; 
                
                // Fire and forget, don't wait for response to avoid jank
                _ = _httpClient.PostAsJsonAsync(url, payload).ContinueWith(task => 
                {
                    if (task.IsFaulted)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LogService] AudioLog failed: {task.Exception?.Message}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[LogService] AudioLog success: {shopId}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LogService] AudioLog error: {ex.Message}");
            }
        }
    }
}
