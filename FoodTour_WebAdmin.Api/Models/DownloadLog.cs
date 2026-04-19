namespace FoodTour_WebAdmin.Api.Models;

public class DownloadLog
{
    public int Id { get; set; }
    public DateTime DownloadedAt { get; set; } = DateTime.UtcNow;
    public string IPAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string VersionDownloaded { get; set; } = "v1.1";

    // Hardware ID của thiết bị mobile — liên kết lượt tải với thiết bị cụ thể
    public string? DeviceId { get; set; } = string.Empty;
}
