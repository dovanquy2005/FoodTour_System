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

            try
            {
                // Determine the correct Dev Tunnel or localhost port here
                // Note for Devs: Replace '7295' with your exact API port
                // Windows (Kestrel): https://localhost:7135
                // Android Emulator: http://10.0.2.2:5154 (HTTP)
                string baseUrl = DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5154" : "https://localhost:7135";
                string requestUrl = $"{baseUrl}/locales/{languageCode}.json";

                // Attempt OTA fetching of the Localization dictionary
                var response = await _httpClient.GetAsync(requestUrl);
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
    }
}
