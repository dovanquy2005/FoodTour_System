using Microsoft.Maui.Controls;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace FoodTour.Mobile.Views
{
    public partial class ScanPage : ContentPage
    {
        private bool _isAnimating;
        
        // Đã bổ sung lại cờ bật/tắt đèn flash
        private bool _isTorchOn = false;

        public ScanPage(ViewModels.ScanViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;

            barcodeReader.Options = new BarcodeReaderOptions
            {
                Formats = BarcodeFormats.All, 
                AutoRotate = true,
                Multiple = false
            };
        }

        // ═══════ LIFECYCLE ═══════

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await RequestCameraPermission();

            viewfinderBorder.Stroke = Color.FromArgb("#E8672A");
            scanLine.Color = Color.FromArgb("#E8672A");
            scanLine.TranslationY = 20;

            _isAnimating = true;
            StartScanLineAnimation();

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(300);

                if (!this.IsVisible) return;

                if (BindingContext is ViewModels.ScanViewModel vm)
                {
                    vm.IsScanning = true;
                }

                if (barcodeReader != null)
                {
                    barcodeReader.IsVisible = false;
                    barcodeReader.IsVisible = true;

                    barcodeReader.CameraLocation = CameraLocation.Front;
                    barcodeReader.CameraLocation = CameraLocation.Rear;

                    // Khôi phục trạng thái đèn flash nếu trước đó đang bật
                    barcodeReader.IsTorchOn = _isTorchOn;

                    barcodeReader.IsDetecting = true;
                }
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            _isAnimating = false;
            scanLine.CancelAnimations();

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
        }

        // ═══════ NÚT BẬT/TẮT ĐÈN FLASH (ĐÃ THÊM LẠI) ═══════
        private void ToggleTorch_Tapped(object? sender, TappedEventArgs e)
        {
            _isTorchOn = !_isTorchOn;
            
            if (barcodeReader != null)
            {
                barcodeReader.IsTorchOn = _isTorchOn;
            }

            // Cập nhật giao diện nút flash
            torchButton.BackgroundColor = _isTorchOn
                ? Color.FromArgb("#E8672A")
                : Color.FromArgb("#44FFFFFF");
            torchIcon.Text = _isTorchOn ? "💡" : "🔦";
        }

        // ═══════ KIỂM TRA QUYỀN CAMERA ═══════

        private async Task RequestCameraPermission()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Lỗi", "Vui lòng cấp quyền sử dụng camera để quét mã QR", "OK");
                }
            }
        }

        // ═══════ ANIMATION QUÉT LASER ═══════

        private void StartScanLineAnimation()
        {
            if (scanLine == null) return;

            var animation = new Animation(v => scanLine.TranslationY = v, 20, 220);
            animation.Commit(this, "ScanLineAnimation", 16, 2000, Easing.Linear, (v, c) =>
            {
                if (_isAnimating)
                {
                    var reverseAnimation = new Animation(v => scanLine.TranslationY = v, 220, 20);
                    reverseAnimation.Commit(this, "ReverseScanLineAnimation", 16, 2000, Easing.Linear, (v, c) =>
                    {
                        if (_isAnimating)
                            StartScanLineAnimation(); 
                    });
                }
            }, () => false);
        }

        // ═══════ XỬ LÝ KẾT QUẢ QUÉT ═══════

        private void BarcodeReader_BarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
        {
            var barcodes = e.Results;
            if (barcodes == null || barcodes.Length == 0) return;

            var firstBarcode = barcodes[0].Value;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (barcodeReader != null)
                {
                    barcodeReader.IsDetecting = false;
                }

                _isAnimating = false;

                viewfinderBorder.Stroke = Colors.Green;
                scanLine.Color = Colors.Green;

                await Task.Delay(500);

                if (BindingContext is ViewModels.ScanViewModel viewModel && viewModel.IsScanning)
                {
                    await viewModel.ProcessQrCodeCommand.ExecuteAsync(firstBarcode);
                }
            });
        }
    }
}