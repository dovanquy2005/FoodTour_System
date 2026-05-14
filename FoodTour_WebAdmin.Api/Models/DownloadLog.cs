namespace FoodTour_WebAdmin.Api.Models;

public class DownloadLog
{
    public int Id { get; set; }
    public DateTime DownloadedAt { get; set; } = DateTime.UtcNow;
    public string IPAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string VersionDownloaded { get; set; } = "v1.1";
    // Đánh giá cấu hình máy: 0 (Mạnh - Cho phép tải), 1 (Yếu - Chặn tải)
    public int DevicePerformanceType { get; set; } = 0;
}
    