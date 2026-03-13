using System.ComponentModel;
using System.Threading.Tasks;

namespace FoodTour.Mobile.Services
{
    /// <summary>
    /// Interface for the dynamic Over-The-Air (OTA) JSON-based Localization System.
    /// Implements INotifyPropertyChanged to automatically trigger UI binding updates when translation language changes.
    /// </summary>
    public interface ILocalizationService : INotifyPropertyChanged
    {
        /// <summary>
        /// Indexer to get a translated string via its key. 
        /// Crucial for dynamic XAML bindings.
        /// </summary>
        /// <param name="key">The translation key</param>
        string this[string key] { get; }

        /// <summary>
        /// Orchestrates fetching the new locale (OTA JSON), caching it for offline use,
        /// and firing the PropertyChanged event to refresh the UI.
        /// </summary>
        /// <param name="languageCode">The target language code (e.g., "en", "vi")</param>
        Task ChangeLanguageAsync(string languageCode);

        /// <summary>
        /// Read the translated text aloud corresponding to the given key, utilizing MAUI's Text-to-Speech API 
        /// while making sure the Locale accurately matches the active language setting.
        /// </summary>
        /// <param name="key">The translation key</param>
        /// <summary>
        /// Read the translated text aloud corresponding to the given key, utilizing MAUI's Text-to-Speech API 
        /// while making sure the Locale accurately matches the active language setting.
        /// </summary>
        /// <param name="key">The translation key</param>
        Task SpeakTextAsync(string key);

        /// <summary>
        /// Pre-downloads all supported languages to ensure offline availability.
        /// </summary>
        Task PreloadAllLanguagesAsync();
    }
}
