namespace FoodTour.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Cách khởi tạo chuẩn: Trả về một Window chứa AppShell
        return new Window(new AppShell());
    }
}