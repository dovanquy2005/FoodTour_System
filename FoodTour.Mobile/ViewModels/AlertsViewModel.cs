using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoodTour.Mobile.Models;
using FoodTour.Mobile.Services;
using FoodTour.Mobile.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace FoodTour.Mobile.ViewModels
{
    /// <summary>
    /// ViewModel cho tab Notification / Alerts.
    /// Quản lý danh sách thông báo cập nhật dữ liệu và xử lý tải bản cập nhật.
    /// </summary>
    public partial class AlertsViewModel : BaseViewModel
    {
        private readonly DatabaseService _dbService;
        private readonly ILocalizationService _localizationService;

        [ObservableProperty]
        private ObservableCollection<NotificationModel> notifications = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isEmpty;

        [ObservableProperty]
        private bool isRefreshing;

        public AlertsViewModel(DatabaseService dbService, ILocalizationService localizationService)
        {
            _dbService = dbService;
            _localizationService = localizationService;
            
            // Đăng ký nhận message để tự động tải lại UI khi mạng WiFi / 4G ngầm phát hiện cập nhật mới
            WeakReferenceMessenger.Default.Register<DataSyncedMessage>(this, async (r, m) =>
            {
                await LoadNotificationsCommand.ExecuteAsync(null);
            });
            WeakReferenceMessenger.Default.Register<NewUpdateAvailableMessage>(this, async (r, m) =>
            {
                await LoadNotificationsCommand.ExecuteAsync(null);
            });
        }

        /// <summary>
        /// Tải danh sách thông báo từ SQLite và hiển thị trên giao diện.
        /// Được gọi khi trang OnAppearing.
        /// </summary>
        [RelayCommand]
        public async Task LoadNotifications()
        {
            try
            {
                IsLoading = true;
                var list = await _dbService.GetNotificationsAsync();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Notifications = new ObservableCollection<NotificationModel>(list);
                    IsEmpty = Notifications.Count == 0;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AlertsVM] Lỗi tải notifications: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;

                // Xóa badge trên AppShell khi đã vào màn hình Alerts
                if (Shell.Current is AppShell appShell)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var alertsTab = appShell.Items.FirstOrDefault()?.Items.FirstOrDefault(t => t.Route == "AlertsPage") as Tab;
                        // Vì "AlertsTab" đã được x:Name, nhưng trong VM khó lấy trực tiếp name. Ta có thể duyệt Tab.
                        // Hoặc Clear toàn bộ badge
                        // Khắc phục nhanh:
                        var atab = appShell.FindByName<Tab>("AlertsTab");
                        if (atab != null)
                        {
                            atab.Title = _localizationService["Tab_Alerts"] ?? "Thông báo";
                        }
                    });
                }
            }
        }

        /// <summary>
        /// Kiểm tra cập nhật mới từ server và làm mới danh sách thông báo.
        /// Sử dụng cho Pull-to-Refresh.
        /// </summary>
        [RelayCommand]
        public async Task RefreshNotifications()
        {
            try
            {
                IsRefreshing = true;

                // Kiểm tra server có bản cập nhật mới không
                if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
                {
                    await _dbService.CheckForUpdatesAsync();
                }

                // Tải lại danh sách sau khi kiểm tra
                await LoadNotifications();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AlertsVM] Lỗi refresh: {ex.Message}");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        /// <summary>
        /// Xóa toàn bộ lịch sử thông báo
        /// </summary>
        [RelayCommand]
        public async Task ClearNotifications()
        {
            if (Shell.Current != null)
            {
                bool confirm = await Shell.Current.DisplayAlert(
                    _localizationService["Alert_Title"] ?? "Xóa thông báo",
                    "Bạn có chắc chắn muốn xóa toàn bộ lịch sử thông báo?",
                    _localizationService["Common_Yes"] ?? "Có",
                    _localizationService["Common_No"] ?? "Không");
                
                if (!confirm) return;
            }

            try
            {
                IsLoading = true;
                await _dbService.ClearAllNotificationsAsync();
                
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Notifications.Clear();
                    IsEmpty = true;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AlertsVM] Lỗi xóa notifications: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Xử lý khi người dùng nhấn nút "Cập nhật ngay" trên một notification.
        /// Kiểm tra mạng → đổi trạng thái → gọi tải dữ liệu → cập nhật UI.
        /// </summary>
        [RelayCommand]
        public async Task DownloadUpdate(NotificationModel? notification)
        {
            if (notification == null) return;

            // Kiểm tra kết nối mạng trước khi tải
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                if (Shell.Current != null)
                {
                    await Shell.Current.DisplayAlert(
                        _localizationService["Common_OK"],
                        _localizationService["Notify_NoNetwork"],
                        _localizationService["Common_OK"]);
                }
                return;
            }

            // Nếu đang dùng 4G/LTE (không phải WiFi), hiển thị prompt cảnh báo data
            bool isWiFi = Helpers.NetworkHelper.IsFreeNetwork();
            if (!isWiFi)
            {
                if (Shell.Current != null)
                {
                    string alertMsg = string.Format(
                        _localizationService["Notify_MobileDataConfirm"] ?? "Có dữ liệu POI mới ({0}), bạn có muốn cập nhật bằng mạng di động không?", 
                        notification.SizeDisplay);

                    bool confirm = await Shell.Current.DisplayAlert(
                        _localizationService["Alert_Title"] ?? "Thông báo",
                        alertMsg,
                        _localizationService["Common_Yes"] ?? "Có",
                        _localizationService["Common_No"] ?? "Không");
                        
                    if (!confirm)
                        return;
                }
            }

            try
            {
                // Cập nhật trạng thái UI ngay lập tức
                notification.Status = "Downloading";
                RefreshNotificationInList(notification);

                // Gọi service tải dữ liệu
                bool success = await _dbService.DownloadUpdateAsync(notification);

                if (success)
                {
                    // Cập nhật UI thành "Đã cập nhật"
                    notification.Status = "Updated";
                    notification.IsDownloaded = true;
                }
                else
                {
                    // Đánh dấu lỗi để người dùng có thể thử lại
                    notification.Status = "Error";
                }

                RefreshNotificationInList(notification);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AlertsVM] Lỗi tải cập nhật: {ex.Message}");

                notification.Status = "Error";
                RefreshNotificationInList(notification);

                // Thông báo lỗi cho người dùng
                if (Shell.Current != null)
                {
                    var message = ex.Message.Contains("transient", StringComparison.OrdinalIgnoreCase)
                        || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                        ? _localizationService["Notify_ServerSleeping"]
                        : _localizationService["Notify_Error"];

                    await Shell.Current.DisplayAlert(
                        _localizationService["Notify_Error"],
                        message,
                        _localizationService["Common_OK"]);
                }
            }
        }

        /// <summary>
        /// Cập nhật lại item trong ObservableCollection để UI refresh binding.
        /// Thay thế item cũ bằng item mới để trigger PropertyChanged.
        /// </summary>
        private void RefreshNotificationInList(NotificationModel notification)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var index = -1;
                for (int i = 0; i < Notifications.Count; i++)
                {
                    if (Notifications[i].Id == notification.Id)
                    {
                        index = i;
                        break;
                    }
                }

                if (index >= 0)
                {
                    Notifications.RemoveAt(index);
                    Notifications.Insert(index, notification);
                }
            });
        }
    }
}