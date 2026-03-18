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

        // Bật/tắt tự động phát audio khi người dùng đến gần quán
        [ObservableProperty]
        private bool isAutoPlay = true;

        // Hiển thị dung lượng lưu trữ offline hiện tại
        [ObservableProperty]
        private string offlineStatus = "—";

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

            // Khôi phục ngôn ngữ đã lưu trước đó
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

            // Bỏ chọn ngôn ngữ cũ
            foreach (var lang in Languages)
            {
                lang.IsSelected = false;
            }

            // Chọn ngôn ngữ mới và lưu vào Preferences
            SelectedLanguageItem.IsSelected = true;
            SelectedLanguage = SelectedLanguageItem.DisplayName;
            string langCode = SelectedLanguageItem.Code;

            Preferences.Default.Set("AppLanguage", langCode);
            await _localizationService.ChangeLanguageAsync(langCode);

            // Quay lại trang trước
            await Shell.Current.GoToAsync("..");
        }

        /// <summary>
        /// Xóa tất cả file ảnh đã cache trên thiết bị.
        /// Hiển thị cảnh báo xác nhận trước khi xóa.
        /// </summary>
        [RelayCommand]
        public async Task ClearImageCache()
        {
            // Xác nhận xóa cache ảnh
            bool confirm = false;
            if (Shell.Current != null)
                confirm = await Shell.Current.DisplayAlert(
                    _localizationService["Settings_ClearConfirmTitle"],
                    _localizationService["Settings_ClearCacheConfirmMsg"],
                    _localizationService["Common_Yes"],
                    _localizationService["Common_No"]);

            if (confirm)
            {
                int deleted = await _dbService.ClearImageCacheAsync();
                OfflineStatus = $"Đã xóa {deleted} ảnh";

                if (Shell.Current != null)
                    await Shell.Current.DisplayAlert(
                        _localizationService["Common_Success"],
                        $"{_localizationService["Settings_ClearCacheDone"]} ({deleted})",
                        _localizationService["Common_OK"]);
            }
        }
    }
}