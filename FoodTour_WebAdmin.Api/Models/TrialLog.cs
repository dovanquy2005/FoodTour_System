using System;

namespace FoodTour_WebAdmin.Api.Models;

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
    
    // Thời điểm mà người dùng nhấn nút Play để nghe thử
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
