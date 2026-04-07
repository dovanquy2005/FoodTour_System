using FoodTour.Mobile.ViewModels;

namespace FoodTour.Mobile.Views;

public partial class AlertsPage : ContentPage
{
    private readonly AlertsViewModel _vm;

    public AlertsPage(AlertsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    /// <summary>
    /// Mỗi lần trang xuất hiện, tải lại danh sách thông báo từ SQLite.
    /// Đảm bảo UI luôn cập nhật khi người dùng quay lại tab.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadNotificationsCommand.ExecuteAsync(null);
    }
}
