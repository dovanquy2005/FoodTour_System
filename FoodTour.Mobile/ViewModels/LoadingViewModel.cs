namespace FoodTour.Mobile.ViewModels
{
    public class LoadingViewModel : BaseViewModel
    {
        private readonly Services.DatabaseService _dbService;

        public LoadingViewModel(Services.DatabaseService dbService)
        {
            _dbService = dbService;
        }

        public async Task InitializeAsync()
        {
            // Khởi tạo Database và đảm bảo dữ liệu đã sẵn sàng
            await _dbService.GetShopsAsync();

            // Đồng bộ dữ liệu ngầm: kiểm tra mạng và đồng bộ không chặn navigation
            // Fire-and-forget: người dùng vẫn được chuyển sang MainTabs ngay lập tức
            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                string apiUrl = "https://foodtour-admin-api.onrender.com";

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

            // Chuyển sang trang chính
            if (Shell.Current != null)
            {
                // Thêm một delay nhỏ để MAUI hoàn tất layout pass trước khi điều hướng, tránh crash ngay lập tức
                await Task.Delay(150);
                await Shell.Current.GoToAsync("//MainTabs");
            }
        }
    }
}