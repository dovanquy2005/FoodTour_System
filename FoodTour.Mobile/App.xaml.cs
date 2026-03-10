namespace FoodTour.Mobile;

public partial class App : Application
{
    public App(FoodTour.Mobile.Services.ILocalizationService localizationService)
    {
        InitializeComponent();

        // Load the persistent language or default to "vi"
        var savedLang = Preferences.Default.Get("AppLanguage", "vi");
        _ = localizationService.ChangeLanguageAsync(savedLang);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Khởi tạo với SplashPage, sau đó chuyển sang AppShell
        return new Window(new Views.SplashPage());
    }
}