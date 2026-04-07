using CommunityToolkit.Mvvm.Messaging;
using FoodTour.Mobile.Messages;

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

            // Đồng bộ dữ liệu ngầm: phân biệt WiFi vs 4G/LTE
            // Fire-and-forget: người dùng vẫn được chuyển sang MainTabs ngay lập tức
            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                bool isWiFi = Helpers.NetworkHelper.IsFreeNetwork();

                string apiUrl = AppConfig.ApiBaseUrl;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (isWiFi)
                        {
                            // ═══════ WiFi: Auto-sync ngầm hoàn toàn ═══════
                            // Tải data mới nhất + media, không hỏi user
                            bool synced = await _dbService.SyncDataFromApiAsync(apiUrl);
                            if (synced)
                            {
                                System.Diagnostics.Debug.WriteLine("Background sync [WiFi]: Đồng bộ ngầm thành công.");

                                // Gửi message → AppShell hiện Snackbar "Dữ liệu đã được cập nhật mới nhất"
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    WeakReferenceMessenger.Default.Send(new DataSyncedMessage("synced"));
                                });
                            }
                        }
                        else
                        {
                            // ═══════ 4G/LTE: Chỉ kiểm tra có update, KHÔNG tự sync ═══════
                            // Tạo notification trong SQLite nếu có dữ liệu mới
                            bool hasUpdate = await _dbService.CheckForUpdatesAsync();
                            System.Diagnostics.Debug.WriteLine($"Background sync [4G]: Kiểm tra cập nhật = {hasUpdate}");

                            if (hasUpdate)
                            {
                                // Gửi message → AppShell hiện Badge đỏ trên tab Alerts
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    WeakReferenceMessenger.Default.Send(new NewUpdateAvailableMessage(0, 0));
                                });
                            }
                        }
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