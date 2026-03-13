using System.Globalization;
using System.Threading.Tasks;

namespace FoodTour.Mobile;

public partial class App : Application
{
    private Page _initialPage = new Views.SplashPage();

    public App(Services.ILocalizationService localizationService, Services.DatabaseService databaseService)
    {
        InitializeComponent();

        InitializeAppAsync(localizationService, databaseService);
    }

    private async void InitializeAppAsync(Services.ILocalizationService localizationService, Services.DatabaseService databaseService)
    {
        // Auto-Detect & Auto-Translate Logic
        // Gọi xuống hệ điều hành máy để hỏi xem máy đang cài ngôn ngữ gì 
        var currentLang = Preferences.Default.Get("AppLanguage", string.Empty);
        
        if (string.IsNullOrEmpty(currentLang))
        {
            var osLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var supportedLangs = new[] { "vi", "en", "ja", "ru", "zh" };
            
            if (Array.Exists(supportedLangs, lang => lang == osLang))
            {
                currentLang = osLang;
            }
            else
            {
                currentLang = "en"; // Fallback
            }
            Preferences.Default.Set("AppLanguage", currentLang);
        }

        // Determine if we should wait for OTA localization
        var isOfflineMode = Preferences.Default.Get("IsOfflineMode", false);
        
        Task locTask;
        if (isOfflineMode)
        {
            // In offline mode, just trigger the change and move on (service uses cache)
            locTask = localizationService.ChangeLanguageAsync(currentLang);
        }
        else
        {
            // In online mode, wait for it (blocks for better UX if server is up)
            locTask = localizationService.ChangeLanguageAsync(currentLang);
        }

        var delayTask = Task.Delay(isOfflineMode ? 500 : 3000); // Shorter splash for offline
        await Task.WhenAll(locTask, delayTask);

        // Safe Routing
        var isSetupCompleted = Preferences.Default.Get("IsSetupCompleted", false);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Page newPage = isSetupCompleted 
                ? new AppShell() 
                : new NavigationPage(new Views.OnboardingPage(localizationService, databaseService));

            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = newPage;
            }
            else
            {
                _initialPage = newPage;
            }
        });
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(_initialPage);
    }
}