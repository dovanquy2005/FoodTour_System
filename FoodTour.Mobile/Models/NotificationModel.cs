using SQLite;
using FoodTour.Mobile.Extensions;

namespace FoodTour.Mobile.Models
{
    /// <summary>
    /// Model thông báo cập nhật dữ liệu - lưu trong SQLite cục bộ.
    /// Mỗi bản ghi đại diện cho một lần phát hiện có dữ liệu mới trên server.
    /// </summary>
    public class NotificationModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>Tiêu đề thông báo (ví dụ: "Có bản cập nhật mới")</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Mô tả chi tiết (ví dụ: "3 quán ăn đã được cập nhật")</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Loại thông báo — hiện tại chỉ dùng "DataUpdate"</summary>
        public string Type { get; set; } = "DataUpdate";

        /// <summary>Tổng dung lượng ước tính (bytes) của bản cập nhật media</summary>
        public long TotalSize { get; set; }

        /// <summary>Đã tải xong hay chưa</summary>
        public bool IsDownloaded { get; set; }

        /// <summary>
        /// Trạng thái hiện tại: "Available" | "Downloading" | "Updated" | "Error"
        /// </summary>
        public string Status { get; set; } = "Available";

        /// <summary>Thời gian tạo thông báo</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Thời gian tạo thông báo (Giờ Việt Nam)
        /// </summary>
        [Ignore]
        public DateTime CreatedAtVN => CreatedAt.ToVietnamTime();

        /// <summary>
        /// Danh sách ID quán ăn cần cập nhật, lưu dạng JSON string
        /// (ví dụ: "[\"s-001\",\"s-003\"]")
        /// </summary>
        public string UpdatedShopIdsJson { get; set; } = "[]";

        /// <summary>
        /// Hiển thị dung lượng dạng MB cho UI binding
        /// </summary>
        [Ignore]
        public string SizeDisplay => TotalSize >= 1_048_576
            ? $"{TotalSize / 1_048_576.0:F1} MB"
            : $"{TotalSize / 1024.0:F0} KB";
    }
}
