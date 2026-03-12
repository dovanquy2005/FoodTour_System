using System.Globalization;
using System.Threading.Tasks;

namespace FoodTour.Mobile;

public partial class App : Application
{
    private Page _initialPage = new Views.SplashPage();

    public App(Services.ILocalizationService localizationService)
    {
        InitializeComponent();

        InitializeAppAsync(localizationService);
    }

    private async void InitializeAppAsync(Services.ILocalizationService localizationService)
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

        // Load translation OTA if available. Combine with a 3-second minimum delay so the Splash Screen can be seen.
        var locTask = localizationService.ChangeLanguageAsync(currentLang);
        var delayTask = Task.Delay(3000);
        await Task.WhenAll(locTask, delayTask);

        // Safe Routing
        var isSetupCompleted = Preferences.Default.Get("IsSetupCompleted", false);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Page newPage = isSetupCompleted 
                ? new AppShell() 
                : new NavigationPage(new Views.OnboardingPage(localizationService));

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