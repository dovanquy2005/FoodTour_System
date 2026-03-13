using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoodTour.Mobile.Services;
using FoodTour.Mobile.Models;
using System.Collections.ObjectModel;

namespace FoodTour.Mobile.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool isAutoPlay = true;

        [ObservableProperty]
        private string radius = "20";

        [ObservableProperty]
        private string offlineStatus = "120MB";

        [ObservableProperty]
        private string appVersion = "1.0.0 (Beta)";

        [ObservableProperty]
        private string selectedLanguage = "Tiếng Việt";

        [ObservableProperty]
        private ObservableCollection<LanguageOption> languages = new();

        [ObservableProperty]
        private LanguageOption? selectedLanguageItem;

        private readonly ILocalizationService _localizationService;
        private readonly DatabaseService _dbService;

        public SettingsViewModel(ILocalizationService localizationService, DatabaseService dbService)
        {
            _localizationService = localizationService;
            _dbService = dbService;
            
            // Resume preferred language
            var savedLang = Preferences.Default.Get("AppLanguage", "vi");
            SelectedLanguage = savedLang switch
            {
                "vi" => "Tiếng Việt",
                "en" => "English",
                "ru" => "Русский",
                "ja" => "日本語",
                "zh" => "中文",
                _ => "Tiếng Việt"
            };

            LoadLanguages(savedLang);
        }

        private void LoadLanguages(string currentLangCode)
        {
            Languages = new ObservableCollection<LanguageOption>
            {
                new LanguageOption { Code = "vi", DisplayName = "Tiếng Việt", FlagIcon = "🇻🇳" },
                new LanguageOption { Code = "en", DisplayName = "English", FlagIcon = "🇬🇧" },
                new LanguageOption { Code = "ru", DisplayName = "Русский", FlagIcon = "🇷🇺" },
                new LanguageOption { Code = "ja", DisplayName = "日本語", FlagIcon = "🇯🇵" },
                new LanguageOption { Code = "zh", DisplayName = "中文", FlagIcon = "🇨🇳" }
            };

            foreach (var lang in Languages)
            {
                if (lang.Code == currentLangCode)
                {
                    lang.IsSelected = true;
                    SelectedLanguageItem = lang;
                }
            }
        }

        [RelayCommand]
        public async Task ChangeLanguage()
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("LanguageSelectionPage");
            }
        }

        [RelayCommand]
        public async Task LanguageSelected()
        {
            if (SelectedLanguageItem == null || Shell.Current == null) return;

            // Deselect previous
            foreach (var lang in Languages)
            {
                lang.IsSelected = false;
            }

            // Select new
            SelectedLanguageItem.IsSelected = true;
            SelectedLanguage = SelectedLanguageItem.DisplayName;
            string langCode = SelectedLanguageItem.Code;

            Preferences.Default.Set("AppLanguage", langCode);
            await _localizationService.ChangeLanguageAsync(langCode);

            // Pop page
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        public async Task UpdateData()
        {
            if (IsBusy) return;
            IsBusy = true;
            OfflineStatus = "Đang đồng bộ...";

            // Determine API URL (Emulator uses 10.0.2.2, Windows uses localhost)
            string apiUrl = DeviceInfo.Platform == DevicePlatform.Android 
                ? "http://10.0.2.2:5154" 
                : "http://localhost:5154";

            bool success = await _dbService.SyncDataFromApiAsync(apiUrl);

            if (success)
            {
                OfflineStatus = "Đã đồng bộ";
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlert(_localizationService["Common_Success"], _localizationService["Settings_UpdateSuccess"], _localizationService["Common_OK"]);
            }
            else
            {
                OfflineStatus = "Lỗi đồng bộ";
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlert("Lỗi", "Không thể cập nhật dữ liệu từ máy chủ.", _localizationService["Common_OK"]);
            }
            
            IsBusy = false;
        }

        [RelayCommand]
        public async Task ClearData()
        {
            bool result = false;
            if (Shell.Current != null)
                result = await Shell.Current.DisplayAlert(_localizationService["Settings_ClearConfirmTitle"], _localizationService["Settings_ClearConfirmMsg"], _localizationService["Common_Yes"], _localizationService["Common_No"]);
            if (result)
            {
                OfflineStatus = "0MB";
            }
        }
    }
}