using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoodTour.Mobile.Services;
using FoodTour.Mobile.Models;
using System.Text.RegularExpressions;

namespace FoodTour.Mobile.ViewModels;

public partial class ScanViewModel : BaseViewModel
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

    /// <summary>
    /// Xử lý nội dung QR đa năng (Universal QR):
    /// - Nếu nội dung là URL (VD: https://foodtour.vn/shop/0262009c-...) → trích GUID bằng Regex.
    /// - Nếu nội dung là GUID thuần → dùng trực tiếp.
    /// - Truy vấn Database theo ID → Phát audio → Đóng trang Scan (Hands-free UX).
    /// </summary>
    [RelayCommand]
    public async Task ProcessQrCodeAsync(string qrContent)
    {
        // Ngăn quét trùng lặp (debounce tầng ViewModel)
        if (!IsScanning) return;

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

            if (shop != null)
            {
                // ── 3. PHÁT AUDIO NGAY (Bypass Cooldown — ưu tiên QR) ──
                await _walkingService.PlayShopFromQrAsync(shop);
                System.Diagnostics.Debug.WriteLine($"[ScanViewModel] Đã kích hoạt audio cho: {shop.Name}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[ScanViewModel] Không tìm thấy quán với ID: {shopId}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ScanViewModel] Lỗi xử lý QR: {ex.Message}");
        }
        finally
        {
            // ── 4. CHUYỂN SANG TAB BẢN ĐỒ (MapPage) ──
            // ScanPage là Root Tab → KHÔNG dùng GoToAsync("..") (sẽ crash).
            // Dùng route tuyệt đối "///MapPage" để nhảy thẳng sang tab Map.
            // BẮT BUỘC chạy trên MainThread vì Shell navigation đụng UI thread.
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
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
}