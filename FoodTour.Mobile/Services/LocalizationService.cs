using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace FoodTour.Mobile.Services
{
    /// <summary>
    /// Dịch vụ bản địa hóa Offline-First: đọc file JSON ngôn ngữ trực tiếp từ app bundle,
    /// không cần tải từ API. Hỗ trợ Text-to-Speech cho nội dung đã dịch.
    /// </summary>
    public class LocalizationService : ILocalizationService
    {
        private Dictionary<string, string> _localizedStrings;
        private string _currentLanguageCode;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Constructor không cần HttpClient vì file JSON được đóng gói sẵn trong app bundle.
        /// </summary>
        public LocalizationService()
        {
            _localizedStrings = new Dictionary<string, string>();
            _currentLanguageCode = "vi"; // Ngôn ngữ mặc định
        }

        /// <summary>
        /// Truy xuất chuỗi dịch theo key. Trả về "[key]" nếu không tìm thấy để dễ phát hiện lỗi thiếu bản dịch.
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

                // Trả về key trong ngoặc vuông để dễ nhận biết thiếu bản dịch
                return $"[{key}]";
            }
        }

        /// <summary>
        /// Đổi ngôn ngữ: đọc file JSON từ thư mục Resources/Raw/locales/ đã đóng gói trong app bundle.
        /// Sử dụng FileSystem.OpenAppPackageFileAsync() để đọc MauiAsset.
        /// Không cần kết nối mạng — hoạt động hoàn toàn offline.
        /// </summary>
        public async Task ChangeLanguageAsync(string languageCode)
        {
            _currentLanguageCode = languageCode;

            try
            {
                // Đọc file JSON từ app bundle (Resources/Raw/locales/)
                // FileSystem.OpenAppPackageFileAsync đọc file MauiAsset đã đóng gói
                var filePath = $"locales/{languageCode}.json";
                using var stream = await FileSystem.OpenAppPackageFileAsync(filePath);
                using var reader = new StreamReader(stream);
                var jsonContent = await reader.ReadToEndAsync();

                // Phân tích JSON thành dictionary các chuỗi dịch
                UpdateDictionaryFromJson(jsonContent);

                // Thông báo UI cập nhật tất cả binding
                NotifyTranslationsChanged();

                Console.WriteLine($"[LocalizationService] Đã tải ngôn ngữ '{languageCode}' từ app bundle.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LocalizationService] Lỗi đọc file ngôn ngữ '{languageCode}': {ex.Message}");
            }
        }

        /// <summary>
        /// Phân tích chuỗi JSON thành Dictionary để lưu trữ các cặp key-value bản dịch.
        /// </summary>
        private void UpdateDictionaryFromJson(string json)
        {
            try
            {
                var options = new JsonSerializerOptions 
                { 
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    PropertyNameCaseInsensitive = true
                };

                var parsedDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json, options);
                
                if (parsedDictionary != null)
                {
                    _localizedStrings = parsedDictionary;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LocalizationService] Lỗi phân tích JSON: {ex.Message}");
            }
        }

        /// <summary>
        /// Thông báo tất cả UI binding rằng ngôn ngữ đã thay đổi, buộc làm mới toàn bộ giao diện.
        /// </summary>
        private void NotifyTranslationsChanged()
        {
            // Đảm bảo chạy trên Main Thread để tránh lỗi cập nhật UI từ background thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // null = tất cả thuộc tính đã thay đổi → MAUI làm mới toàn bộ binding
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
                
                // Thông báo riêng cho indexer binding
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            });
        }

        /// <summary>
        /// Đọc text đã dịch và phát âm bằng Text-to-Speech với locale phù hợp.
        /// </summary>
        public async Task SpeakTextAsync(string key)
        {
            var textToSpeak = this[key];

            try
            {
                var locales = await TextToSpeech.Default.GetLocalesAsync();
                Locale? matchingLocale = null;

                // Tìm locale TTS phù hợp với ngôn ngữ hiện tại
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
                Console.WriteLine($"[LocalizationService] Lỗi TTS: {ex.Message}");
            }
        }

        /// <summary>
        /// Không cần tải trước ngôn ngữ nữa vì file JSON đã nằm trong app bundle.
        /// Giữ phương thức này để tương thích interface, nhưng không làm gì cả.
        /// </summary>
        public Task PreloadAllLanguagesAsync()
        {
            // File JSON đã đóng gói trong app — không cần tải từ server
            Console.WriteLine("[LocalizationService] PreloadAllLanguagesAsync: Bỏ qua — file đã có sẵn trong app bundle.");
            return Task.CompletedTask;
        }
    }
}
