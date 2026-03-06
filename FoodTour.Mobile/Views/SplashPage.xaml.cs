namespace FoodTour.Mobile.Views;

public partial class SplashPage : ContentPage
{
	public SplashPage()
	{
		InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // 1. Tạo một độ trễ giả lập (ví dụ: 3 giây) để người dùng kịp nhìn màn hình splash
        await Task.Delay(3000); // 3000 ms = 3 giây

        // 2. Chuyển hướng đến trang chính thực sự của ứng dụng
        if (Application.Current?.Windows.Count > 0)
        {
            Application.Current.Windows[0].Page = new AppShell();
        }
    }
}