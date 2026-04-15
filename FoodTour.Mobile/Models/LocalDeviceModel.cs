using SQLite;

namespace FoodTour.Mobile.Models;

/// <summary>
/// Lưu thông tin thiết bị cục bộ trong SQLite.
/// Bảng này chỉ có duy nhất 1 dòng — được tạo lần đầu khi App khởi động.
/// DeviceId sẽ được đồng bộ lên Backend qua API /api/device/sync.
/// </summary>
[Table("LocalDevice")]
public class LocalDeviceModel
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>GUID bền vững, định danh thiết bị này với hệ thống Backend.</summary>
    [NotNull]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>Tên model máy (DeviceInfo.Model) — hiển thị trên Web Admin.</summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>Thời điểm tạo bản ghi lần đầu (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
