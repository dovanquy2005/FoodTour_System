using System;

namespace FoodTour_WebAdmin.Api.Models;

/// <summary>
/// Phân loại nguồn kích hoạt nghe thử:
/// Web = 0 — Khách vãng lai quét QR trên trình duyệt (mặc định, tương thích dữ liệu cũ).
/// AppScan = 1 — Du khách chủ động quét QR bên trong App.
/// AppAuto = 2 — Du khách đi vào vùng Radius, App tự động phát thuyết minh.
/// </summary>
public enum TriggerType
{
    Web = 0,
    AppScan = 1,
    AppAuto = 2
}

/// <summary>
/// Bảng lưu vết số lần nghe thử (Trial) dựa trên IP của người dùng.
/// Giúp chống việc khách dùng Tab Ẩn danh (Incognito) để qua mặt logic localStorage.
/// </summary>
public class TrialLog
{
    public int Id { get; set; }
    
    // Địa chỉ IP của máy khách (người nghe)
    public string IPAddress { get; set; } = string.Empty;
    
    // Lưu tạm User-Agent để nhận dạng thêm thiết bị (tránh block lầm người dùng chung IP mạng)
    public string UserAgent { get; set; } = string.Empty;

    // Hardware ID của thiết bị mobile (AndroidId) — dùng để kiểm soát trial chính xác hơn IP
    public string? DeviceId { get; set; } = string.Empty;

    // ID của quán đã nghe thử — ghi log shop nào được nghe
    public string? ShopId { get; set; } = string.Empty;
    
    // Thời điểm mà người dùng nhấn nút Play để nghe thử
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? BrowserFingerprint { get; set; }

    // Nguồn kích hoạt: Web (mặc định), AppScan, AppAuto — phục vụ Analytics phân loại
    public TriggerType TriggerType { get; set; } = TriggerType.Web;
}
