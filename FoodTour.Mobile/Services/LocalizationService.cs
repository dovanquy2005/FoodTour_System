using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace FoodTour.Mobile.Services
{
    /// <summary>
    /// Implementation of ILocalizationService that provides Over-The-Air text translation capabilities, 
    /// fallback caching strategy for offline support, and native Text-to-Speech translation output.
    /// Uses DI for injecting HttpClient and strictly follows Clean Architecture by abstracting IO operations.
    /// </summary>
    public class LocalizationService : ILocalizationService
    {
        private readonly HttpClient _httpClient;
        private Dictionary<string, string> _localizedStrings;
        private string _currentLanguageCode;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Service constructor injecting HttpClient. 
        /// Uses DI to maintain loose coupling while taking care of network abstractions.
        /// </summary>
        public LocalizationService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _localizedStrings = new Dictionary<string, string>();
            _currentLanguageCode = "en"; // Base fallback language
        }

        /// <summary>
        /// Indexer for fetching localized strings dynamically. 
        /// Will return $"[{key}]" gracefully if the string does not exist, alerting missing localized entries.
        /// </summary>
        public string this[string key]
        {
            get
            {
                if (string.IsNullOrEmpty(key)) return string.Empty;

                if (_localizedStrings != null && _localizedStrings.TryGetValue(key, out var localizedValue))
                {
                    return localizedValue;
                }

                // Exception handler constraint: graceful miss
                return $"[{key}]";
            }
        }

        /// <summary>
        /// Retrieves translation dictionary OTA and handles fallback cache management.
        /// </summary>
        public async Task ChangeLanguageAsync(string languageCode)
        {
            _currentLanguageCode = languageCode;
            var fileName = $"locale_{languageCode}.json";
            
            // FileSystem.AppDataDirectory securely persists across app launches 
            var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

            // Kiểm tra xem có đang ở chế độ Offline không
            bool isOfflineMode = Preferences.Default.Get("IsOfflineMode", false);

            if (isOfflineMode && File.Exists(filePath))
            {
                // Load ngay lập tức từ cache nếu đang chạy Offline
                try
                {
                    var cachedJson = await File.ReadAllTextAsync(filePath);
                    UpdateDictionaryFromJson(cachedJson);
                    NotifyTranslationsChanged();
                    Console.WriteLine($"[LocalizationService] Applied cached translation for '{languageCode}' (Offline Mode).");
                    return; // Thoát sớm, không cần gọi API
                }
                catch (Exception fileException)
                {
                    Console.WriteLine($"[LocalizationService] Cache retrieval failed: {fileException.Message}");
                    // Nếu lỗi đọc file, tiếp tục thử tải từ API
                }
            }

            try
            {
                // Determine the correct Dev Tunnel or localhost port here
                string baseUrl = DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5154" : "http://localhost:5154";
                string requestUrl = $"{baseUrl}/locales/{languageCode}.json";

                // Thay vì dùng _httpClient mặc định, ta dùng 1 client với Timeout rất ngắn (ví dụ 1.5s)
                // để tránh giật lag UI khi server sập nhưng chưa kịp bật Offline mode
                using var quickClient = new HttpClient { Timeout = TimeSpan.FromSeconds(1.5) };

                var response = await quickClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();

                // Save JSON document directly to AppDataDirectory for offline operation
                await File.WriteAllTextAsync(filePath, jsonContent);

                UpdateDictionaryFromJson(jsonContent);
                NotifyTranslationsChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LocalizationService] OTA Update failed: {ex.Message}. Attempting to use offline cache...");

                // Fallback operation to retrieve the cached target language
                if (File.Exists(filePath))
                {
                    try
                    {
                        var cachedJson = await File.ReadAllTextAsync(filePath);
                        UpdateDictionaryFromJson(cachedJson);
                        NotifyTranslationsChanged();
                        Console.WriteLine($"[LocalizationService] Applied cached translation for '{languageCode}'.");
                    }
                    catch (Exception fileException)
                    {
                        Console.WriteLine($"[LocalizationService] Cache retrieval failed: {fileException.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"[LocalizationService] No cache file found for language '{languageCode}'.");
                }
            }
        }

        private void UpdateDictionaryFromJson(string json)
        {
            try
            {
                var options = new JsonSerializerOptions 
                { 
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    PropertyNameCaseInsensitive = true
                };

                // Utilizing System.Text.Json to perform an efficient deserialization 
                var parsedDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json, options);
                
                if (parsedDictionary != null)
                {
                    _localizedStrings = parsedDictionary;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LocalizationService] Error parsing JSON translation map: {ex.Message}");
            }
        }

        private void NotifyTranslationsChanged()
        {
            // By wrapping with MainThread.BeginInvokeOnMainThread, we guarantee that 
            // the UI elements safely receive the property changed updates avoiding silent background thread failures.
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Passing null signals MAUI bindings that ALL properties have changed, forcing a total refresh
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
                
                // Specific signal for indexer binding updates
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            });
        }

        /// <summary>
        /// Looks up OS TTS Localizations and attempts to audibly output the matching translated text. 
        /// </summary>
        public async Task SpeakTextAsync(string key)
        {
            var textToSpeak = this[key];

            try
            {
                var locales = await TextToSpeech.Default.GetLocalesAsync();
                Locale? matchingLocale = null;

                // Match specific localization language using ISO logic
                foreach (var locale in locales)
                {
                    if (locale.Language.StartsWith(_currentLanguageCode, StringComparison.OrdinalIgnoreCase))
                    {
                        matchingLocale = locale;
                        break;
                    }
                }

                var options = new SpeechOptions
                {
                    Locale = matchingLocale
                };

                await TextToSpeech.Default.SpeakAsync(textToSpeak, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LocalizationService] TTS Execution failed: {ex.Message}");
            }
        }
        /// <summary>
        /// Pre-downloads all supported languages to ensure offline availability.
        /// </summary>
        public async Task PreloadAllLanguagesAsync()
        {
            var supportedLangs = new[] { "vi", "en", "ja", "ru", "zh" };
            string baseUrl = DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5154" : "http://localhost:5154";
            
            // Dùng client timeout ngắn để không bị treo nếu lỗi mạng
            using var quickClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

            foreach (var lang in supportedLangs)
            {
                var fileName = $"locale_{lang}.json";
                var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                string requestUrl = $"{baseUrl}/locales/{lang}.json";

                try
                {
                    var response = await quickClient.GetAsync(requestUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonContent = await response.Content.ReadAsStringAsync();
                        await File.WriteAllTextAsync(filePath, jsonContent);
                        Console.WriteLine($"[LocalizationService] Preloaded language '{lang}'.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LocalizationService] Preload failed for '{lang}': {ex.Message}");
                }
            }
        }
    }
}
