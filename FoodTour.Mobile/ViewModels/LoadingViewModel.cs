namespace FoodTour.Mobile.ViewModels
{
    public class LoadingViewModel : BaseViewModel
    {
        private readonly Services.DatabaseService _dbService;

        public LoadingViewModel(Services.DatabaseService dbService)
        {
            _dbService = dbService;
            CheckAndLoadData();
        }

        private async void CheckAndLoadData()
        {
            // Khởi tạo Database và Seed dữ liệu mẫu
            await _dbService.GetPoisAsync();
            await Task.Delay(1500); // Thêm một chút delay để user kịp nhìn màn hình splash

            await Shell.Current.GoToAsync("//MainTabs");
        }
    }
}