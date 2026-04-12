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

        // Reset giao diện khung ngắm về trạng thái ban đầu (phòng trường hợp quay lại sau lần quét trước)
        viewfinderBorder.Stroke = Color.FromArgb("#E8672A");
        scanLine.Color = Color.FromArgb("#E8672A");
        scanLine.TranslationY = 20;

        // Bắt đầu animation đường quét laser
        _isAnimating = true;
        StartScanLineAnimation();

        // ★ "Bí thuật Delay" — Kích hoạt camera có trễ để tránh Black Screen
        // ZXing.Net.Maui trên Android cần thời gian để Driver Camera khởi động phần cứng.
        // Nếu gán IsDetecting = true quá sớm, ống kính chưa sẵn sàng → màn hình đen.
        // Reset IsScanning TRONG MainThread block để đồng bộ với lúc camera bật,
        // tránh race condition giữa ViewModel state và hardware callback.
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // Nhường 300ms cho Driver Camera hoàn tất khởi tạo phần cứng
            await Task.Delay(300);

            // Reset trạng thái quét — đặt bên trong MainThread để sync với camera activation
            if (BindingContext is ScanViewModel vm)
                vm.IsScanning = true;

            if (barcodeReader != null)
            {
                // Ép đánh thức ống kính bằng cách gán lại CameraLocation về camera sau (Rear)
                barcodeReader.CameraLocation = ZXing.Net.Maui.CameraLocation.Rear;

                // Bật cờ quét SAU KHI phần cứng đã sẵn sàng
                barcodeReader.IsDetecting = true;
            }
        });
    }

    // Tắt camera + dừng animation khi rời khỏi Tab (tiết kiệm pin, giải phóng tài nguyên)
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Dừng animation đường quét trước (không phụ thuộc MainThread)
        _isAnimating = false;

        // Ngắt phần cứng camera an toàn trên MainThread
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (barcodeReader != null)
            {
                barcodeReader.IsDetecting = false;

                // Tắt đèn flash nếu đang bật (tránh sáng liên tục khi chuyển tab)
                if (_isTorchOn)
                {
                    _isTorchOn = false;
                    barcodeReader.IsTorchOn = false;
                    torchButton.BackgroundColor = Color.FromArgb("#44FFFFFF");
                    torchIcon.Text = "🔦";
                }
            }
        });
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
                // ★ BƯỚC 1 — TẮT CAMERA ĐẦU TIÊN, NGAY LẬP TỨC
                // Phải là lệnh đầu tiên để ngăn event BarcodesDetected fire liên tục → chống quét lặp & treo máy
                barcodeReader.IsDetecting = false;

                // ★ BƯỚC 2 — Dừng animation đường quét ngay sau đó
                _isAnimating = false;

                // ★ BƯỚC 3 — Hiệu ứng phản hồi thành công: đổi màu khung ngắm sang xanh lá
                viewfinderBorder.Stroke = Color.FromArgb("#5DAA68");
                scanLine.Color = Color.FromArgb("#5DAA68");

                // Chờ 0.5s để người dùng nhận biết quét thành công trước khi xử lý
                await Task.Delay(500);

                // Gọi ViewModel xử lý nội dung QR (dùng ExecuteAsync cho an toàn bất đồng bộ)
                await viewModel.ProcessQrCodeCommand.ExecuteAsync(first.Value);
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