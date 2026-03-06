namespace FoodTour.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Khởi tạo với SplashPage, sau đó chuyển sang AppShell
        return new Window(new Views.SplashPage());
    }
}