using System.ComponentModel.DataAnnotations.Schema;

namespace FoodTour_WebAdmin.Api.Models;

/// <summary>
/// Trạng thái duyệt của một yêu cầu chỉnh sửa quán từ Owner.
/// </summary>
public enum SubmissionStatus
{
    Pending,   // Đang chờ Admin duyệt
    Approved,  // Đã duyệt — Admin đã đồng bộ sang bảng Shops
    Rejected   // Bị từ chối — Owner có thể sửa lại và gửi yêu cầu mới
}

/// <summary>
/// Bảng trung gian lưu các yêu cầu cập nhật thông tin quán từ Chủ Quán (Owner).
/// Dữ liệu chỉ được áp dụng vào bảng Shops sau khi Admin phê duyệt.
/// </summary>
public class ShopSubmission
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // ── Liên kết ──
    /// <summary>ID của quán đang yêu cầu cập nhật (FK → Shops.Id)</summary>
    public string ShopId { get; set; } = string.Empty;

    /// <summary>ID của chủ quán gửi yêu cầu (FK → Users.Id)</summary>
    public string OwnerId { get; set; } = string.Empty;

    // Navigation Properties
    [ForeignKey("OwnerId")]
    public virtual UserModel? Owner { get; set; }

    [ForeignKey("ShopId")]
    public virtual ShopModel? Shop { get; set; }

    // ── Nội dung đề xuất cập nhật (mirror các trường của ShopModel) ──
    public string ImageUrl { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Radius { get; set; }
    public int Priority { get; set; }
    public double Rating { get; set; }

    // Thông tin dịch (chỉ tiếng Việt, đủ cho Owner tự chỉnh sửa)
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // ── Trạng thái & Metadata ──
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;

    /// <summary>Ghi chú của Admin khi Reject (tùy chọn)</summary>
    public string? AdminNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
