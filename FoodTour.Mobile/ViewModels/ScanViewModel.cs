using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoodTour.Mobile.Services;
using FoodTour.Mobile.Models;
using System.Text.RegularExpressions;

namespace FoodTour.Mobile.ViewModels;

public partial class ScanViewModel : BaseViewModel, IDisposable
{
    private readonly WalkingSimulationService _walkingService;
    private readonly DatabaseService _dbService;

    [ObservableProperty]
    private bool isScanning = true;


    // Regex để trích xuất GUID từ URL hoặc chuỗi thô
    // Hỗ trợ cả URL dạng https://foodtour.vn/shop/{guid} hay https://domain/foodtour/{guid}
    // và cả trường hợp nội dung QR là GUID thuần túy
    private static readonly Regex GuidRegex = new(
        @"([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
        RegexOptions.Compiled);

    public ScanViewModel(WalkingSimulationService walkingService, DatabaseService dbService)
    {
        _walkingService = walkingService;
        _dbService = dbService;
    }

    // Enum TriggerType constant: AppScan = 1
    private const int TriggerTypeAppScan = 1;

    /// <summary>
    /// Xử lý nội dung QR đa năng (Universal QR):
    /// - Nếu nội dung là URL (VD: https://foodtour.vn/shop/0262009c-...) → trích GUID bằng Regex.
    /// - Nếu nội dung là GUID thuần → dùng trực tiếp.
    /// - Kiểm tra quyền Trial/Premium → Phát audio → Chuyển sang tab Bản đồ.
    /// </summary>
    [RelayCommand]
    public async Task ProcessQrCodeAsync(string qrContent)
    {
        // Ngăn quét trùng lặp (debounce tầng ViewModel)
        if (!IsScanning) return;

        string? trialAlertMessage = null;

        try
        {
            // Tạm dừng cờ quét — chặn barcode callback chạy thêm lần nữa
            IsScanning = false;

            // ── 1. TRÍCH XUẤT SHOP ID TỪ NỘI DUNG QR ──
            string? shopId = ExtractShopId(qrContent);

            if (string.IsNullOrEmpty(shopId))
            {
                System.Diagnostics.Debug.WriteLine($"[ScanViewModel] Không tìm thấy GUID trong QR: {qrContent}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[ScanViewModel] Trích xuất Shop ID: {shopId}");

            // ── 2. TRUY VẤN SHOP TỪ DATABASE (đúng ngôn ngữ hiện tại) ──
            var shop = await _dbService.GetShopAsync(shopId);

            if (shop == null)
            {
                System.Diagnostics.Debug.WriteLine($"[ScanViewModel] Không tìm thấy quán với ID: {shopId}");
                return;
            }

            // ── 3. KIỂM TRA QUYỀN TRIAL/PREMIUM TRƯỚC KHI PHÁT ──
            bool shouldPlay = true;
            var deviceId = App.DeviceId;

            if (!string.IsNullOrEmpty(deviceId))
            {
                try
                {
                    // Gọi API ghi log trial với TriggerType = AppScan (1)
                    // API sẽ tự kiểm tra: nếu đã hết lượt → trả về allowed=false
                    var trialResult = await _dbService.RecordTrialAsync(deviceId, shop.Id, TriggerTypeAppScan);

                    if (trialResult != null && !trialResult.Allowed)
                    {
                        // Đã hết 3 lượt quét QR chủ động → chặn phát audio
                        shouldPlay = false;
                        trialAlertMessage = "Bạn đã hết 3 lượt quét mã chủ động trong 24h.\n\n" +
                            "💡 Hãy nâng cấp Premium để nghe không giới hạn, " +
                            "hoặc sử dụng tính năng tự động thuyết minh trên tab Bản đồ (miễn phí).";
                        System.Diagnostics.Debug.WriteLine($"[ScanViewModel] QR Trial limit reached for device: {deviceId}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[ScanViewModel] Trial recorded OK. Remaining: {trialResult?.Remaining}");
                    }
                }
                catch (Exception ex)
                {
                    // Lỗi mạng → cho phép phát (fail-open) để không làm hỏng UX
                    System.Diagnostics.Debug.WriteLine($"[ScanViewModel] Trial check error (fail-open): {ex.Message}");
                }
            }

            // ── 4. PHÁT AUDIO NẾU ĐƯỢC PHÉP ──
            if (shouldPlay)
            {
                await _walkingService.PlayShopFromQrAsync(shop);
                System.Diagnostics.Debug.WriteLine($"[ScanViewModel] Đã kích hoạt audio cho: {shop.Name}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ScanViewModel] Lỗi xử lý QR: {ex.Message}");
        }
        finally
        {
            // ── 5. HIỂN THỊ THÔNG BÁO (nếu có) rồi CHUYỂN TRANG ──
            var alertMsg = trialAlertMessage; // Capture for lambda
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    // Nếu hết lượt trial, hiển thị alert trước khi chuyển trang
                    if (!string.IsNullOrEmpty(alertMsg))
                    {
                        if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
                        {
                            await Application.Current.Windows[0].Page!.DisplayAlert(
                                "Hết lượt nghe thử",
                                alertMsg,
                                "Đã hiểu");
                        }
                    }

                    await Shell.Current.GoToAsync("///MapPage");
                }
                catch (Exception navEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[ScanViewModel] Lỗi điều hướng: {navEx.Message}");
                }
            });
        }
    }


    /// <summary>
    /// Trích xuất chuỗi GUID (Shop ID) từ nội dung QR.
    /// Hỗ trợ cả URL lẫn GUID thuần túy.
    /// VD: "https://foodtour.vn/shop/0262009c-1693-4a77-95e7-d888247b906d" → "0262009c-1693-4a77-95e7-d888247b906d"
    /// VD: "0262009c-1693-4a77-95e7-d888247b906d" → giữ nguyên
    /// </summary>
    private static string? ExtractShopId(string qrContent)
    {
        if (string.IsNullOrWhiteSpace(qrContent))
            return null;

        var match = GuidRegex.Match(qrContent);
        return match.Success ? match.Groups[1].Value : null;
    }

    public void Dispose()
    {
        // Reserved for future cleanup if needed
    }
}