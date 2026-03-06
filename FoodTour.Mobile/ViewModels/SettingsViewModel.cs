using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FoodTour.Mobile.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        [ObservableProperty]
        private bool isAutoPlay = true;

        [ObservableProperty]
        private string radius = "20";

        [ObservableProperty]
        private string offlineStatus = "Đã tải (120MB)";

        [ObservableProperty]
        private string appVersion = "1.0.0 (Beta)";

        public SettingsViewModel()
        {
        }

        [RelayCommand]
        public async Task UpdateData()
        {
            OfflineStatus = "Đang tải dữ liệu...";
            await Task.Delay(1500); // Simulate download
            OfflineStatus = "Đã cập nhật (125MB)";
            await Application.Current.MainPage.DisplayAlert("Thành công", "Đã cập nhật dữ liệu Offline mới nhất", "OK");
        }

        [RelayCommand]
        public async Task ClearData()
        {
            bool result = await Application.Current.MainPage.DisplayAlert("Xóa dữ liệu", "Bạn có chắc chắn muốn xóa dữ liệu offline?", "Xóa", "Hủy");
            if (result)
            {
                OfflineStatus = "Trống (0MB)";
            }
        }
    }
}