using FoodTour.Mobile.ViewModels;
using ZXing.Net.Maui;

namespace FoodTour.Mobile.Views;

public partial class ScanPage : ContentPage
{
    // Cờ bật/tắt đèn flash
    private bool _isTorchOn = false;
    // Cờ ngăn animation chạy lại khi trang đã tắt
    private bool _isAnimating = false;

    public ScanPage(ScanViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // ═══════ LIFECYCLE ═══════
    // Mỗi khi trang hiện lên: yêu cầu quyền camera + khởi chạy animation đường quét
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RequestCameraPermission();

        // Reset trạng thái quét khi quay lại trang
        if (BindingContext is ScanViewModel vm)
            vm.IsScanning = true;

        // Bắt đầu animation đường quét laser
        _isAnimating = true;
        StartScanLineAnimation();
    }

    // Dừng animation khi rời khỏi trang (tránh leak)
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isAnimating = false;
    }

    // ═══════ XIN QUYỀN CAMERA ═══════
    private async Task RequestCameraPermission()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();

        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<Permissions.Camera>();

        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Thông báo",
                "Vui lòng cấp quyền Camera để sử dụng chức năng quét mã QR.",
                "Đã hiểu");
            await Shell.Current.GoToAsync("..");
        }
    }

    // ═══════ ANIMATION ĐƯỜNG QUÉT (Scan Line) ═══════
    // Đường gạch ngang chạy lên xuống liên tục bên trong khung ngắm 260px
    private async void StartScanLineAnimation()
    {
        while (_isAnimating)
        {
            // Di chuyển từ trên (20px) xuống dưới (230px) trong 1.5 giây
            await scanLine.TranslateTo(0, 230, 1500, Easing.SinInOut);
            if (!_isAnimating) break;

            // Di chuyển ngược lại lên trên
            await scanLine.TranslateTo(0, 20, 1500, Easing.SinInOut);
        }
    }

    // ═══════ XỬ LÝ SỰ KIỆN QUÉT THÀNH CÔNG ═══════
    private void BarcodeReader_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var first = e.Results?.FirstOrDefault();
        if (first == null) return;

        if (BindingContext is ScanViewModel viewModel && viewModel.IsScanning)
        {
            Dispatcher.Dispatch(async () =>
            {
                // Hiệu ứng phản hồi: đổi viền khung ngắm sang màu xanh lá (Success)
                viewfinderBorder.Stroke = Color.FromArgb("#5DAA68");
                scanLine.Color = Color.FromArgb("#5DAA68");

                // Dừng animation đường quét
                _isAnimating = false;

                // Chờ 0.5s để người dùng nhận biết quét thành công
                await Task.Delay(500);

                // Gọi ViewModel xử lý nội dung QR
                viewModel.ProcessQrCodeCommand.Execute(first.Value);
            });
        }
    }

    // ═══════ NÚT BẬT/TẮT ĐÈN FLASH ═══════
    private void ToggleTorch_Tapped(object? sender, TappedEventArgs e)
    {
        _isTorchOn = !_isTorchOn;
        barcodeReader.IsTorchOn = _isTorchOn;

        // Cập nhật giao diện nút flash
        torchButton.BackgroundColor = _isTorchOn
            ? Color.FromArgb("#E8672A")
            : Color.FromArgb("#44FFFFFF");
        torchIcon.Text = _isTorchOn ? "💡" : "🔦";
    }
}