using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FoodTour.Mobile.Services;
using FoodTour.Mobile.Models;
using FoodTour.Mobile.Messages;
using System.Collections.ObjectModel;

namespace FoodTour.Mobile.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        [ObservableProperty]
        private bool isBusy;



        [ObservableProperty]
        private string appVersion = "1.0.0 (Beta)";

        [ObservableProperty]
        private bool isPremium;

        [ObservableProperty]
        private string selectedLanguage = "Tiếng Việt";

        [ObservableProperty]
        private ObservableCollection<LanguageOption> languages = new();

        [ObservableProperty]
        private LanguageOption? selectedLanguageItem;

        private readonly ILocalizationService _localizationService;
        private readonly DatabaseService _dbService;
#if ANDROID
        private readonly IHardwareIdService _hardwareIdService;
#endif

        public SettingsViewModel(ILocalizationService localizationService, DatabaseService dbService
#if ANDROID
            , IHardwareIdService hardwareIdService
#endif
            )
        {
            _localizationService = localizationService;
            _dbService = dbService;
#if ANDROID
            _hardwareIdService = hardwareIdService;
#endif
            _ = CheckPremiumStatusAsync();

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

            // Bắn tín hiệu toàn cục thông báo ngôn ngữ vừa bị thay đổi
            WeakReferenceMessenger.Default.Send(new LanguageChangedMessage(langCode));

            // Quay lại trang trước
            await Shell.Current.GoToAsync("..");
        }



        [RelayCommand]
        public async Task UpgradePremium()
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                if (Shell.Current != null)
                {
                    await Shell.Current.DisplayAlert(
                        "Lỗi kết nối",
                        "Vui lòng kết nối mạng để thực hiện giao dịch nâng cấp Premium.",
                        "Đóng");
                }
                return;
            }

            // TODO: Tích hợp cổng thanh toán (Payment Gateway) thực tế tại đây
            System.Diagnostics.Debug.WriteLine("[Settings] Chuyển hướng thanh toán Premium...");
            
            if (Shell.Current != null)
            {
                await Shell.Current.DisplayAlert(
                    "Thanh toán",
                    "Tính năng thanh toán đang được phát triển. Vui lòng thử lại sau.",
                    "OK");
            }
        }

        private async Task CheckPremiumStatusAsync()
        {
#if ANDROID
            try
            {
                var hId = _hardwareIdService.GetHardwareId();
                var status = await _dbService.CheckDeviceStatusAsync(hId);
                if (status != null)
                {
                    IsPremium = status.IsPremium;
                }
            }
            catch { }
#endif
        }
    }
}