using System.Globalization;
using System.Threading.Tasks;

namespace FoodTour.Mobile;

public partial class App : Application
{
    public App(Services.ILocalizationService localizationService)
    {
        InitializeComponent();

        // Prevent UI flash by using a blank page initially
        MainPage = new ContentPage { BackgroundColor = Color.FromArgb("#120E0E") }; // Uses your Base Background or Black

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

        // Apply translation blockingly to memory before routing
        await localizationService.ChangeLanguageAsync(currentLang);

        // Safe Routing
        var isSetupCompleted = Preferences.Default.Get("IsSetupCompleted", false);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (isSetupCompleted)
            {
                MainPage = new AppShell();
            }
            else
            {
                MainPage = new NavigationPage(new Views.OnboardingPage(localizationService));
            }
        });
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(MainPage);
    }
}