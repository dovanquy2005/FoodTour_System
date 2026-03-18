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
            // Khởi tạo Database và đảm bảo dữ liệu đã sẵn sàng
            await _dbService.GetShopsAsync();

            // Đồng bộ dữ liệu ngầm: kiểm tra mạng và đồng bộ không chặn navigation
            // Fire-and-forget: người dùng vẫn được chuyển sang MainTabs ngay lập tức
            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                string apiUrl = DeviceInfo.Platform == DevicePlatform.Android
                    ? "http://10.0.2.2:5154"
                    : "http://localhost:5154";

                // Bắt đầu đồng bộ ngầm — không await để không chặn UI
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _dbService.SyncDataFromApiAsync(apiUrl);
                        System.Diagnostics.Debug.WriteLine("Background sync: Đồng bộ dữ liệu ngầm thành công.");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Background sync error: {ex.Message}");
                    }
                });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Background sync: Không có mạng, bỏ qua đồng bộ ngầm.");
            }

            // Chuyển sang trang chính ngay lập tức — không chờ đồng bộ hoàn tất
            await Shell.Current.GoToAsync("//MainTabs");
        }
    }
}