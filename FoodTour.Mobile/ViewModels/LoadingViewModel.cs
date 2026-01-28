namespace FoodTour.Mobile.ViewModels
{
    public class LoadingViewModel : BaseViewModel
    {
        public LoadingViewModel()
        {
            CheckAndLoadData();
        }

        private async void CheckAndLoadData()
        {
            // Giả lập tải data (sau này sẽ thay bằng code tải SQLite thật)
            await Task.Delay(3000); // Đợi 3 giây

            // Chuyển hướng sang màn hình Map (Route: //MainTabs)
            // Lưu ý: Route này phải khớp với cái đặt trong AppShell.xaml
            await Shell.Current.GoToAsync("//MainTabs");
        }
    }
}