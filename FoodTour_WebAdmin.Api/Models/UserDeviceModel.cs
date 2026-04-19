namespace FoodTour_WebAdmin.Api.Models;

/// <summary>
/// Lưu thông tin thiết bị mobile đã đồng bộ lên hệ thống.
/// DeviceId là GUID bất biến được tạo lần đầu trên máy người dùng.
/// </summary>
public class UserDeviceModel
{
    public int Id { get; set; }

    /// <summary>GUID được tạo trên Mobile, dùng làm khóa định danh duy nhất.</summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>Tên thiết bị (Model máy hoặc do người dùng đặt).</summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>Nền tảng hệ điều hành (Android, iOS, v.v.).</summary>
    public string Platform { get; set; } = "Unknown";

    /// <summary>Lần cuối thiết bị gọi API sync.</summary>
    public DateTime LastActive { get; set; } = DateTime.UtcNow;

    /// <summary>Đánh dấu thiết bị có quyền nghe toàn bộ audio (Premium Pass).</summary>
    public bool IsPremium { get; set; } = false;

    /// <summary>Thời điểm hết hạn Premium (null = chưa mua hoặc vĩnh viễn).</summary>
    public DateTime? PremiumExpiry { get; set; }

    // ── Quan hệ 1-nhiều với UserModel (nullable — thiết bị có thể chưa đăng nhập) ──
    public string? UserId { get; set; }
    public UserModel? User { get; set; }
}
