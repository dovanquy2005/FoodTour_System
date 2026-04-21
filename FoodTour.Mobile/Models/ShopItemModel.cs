using SQLite;

namespace FoodTour.Mobile.Models
{
    /// <summary>
    /// Model SQLite cho nội dung độc quyền (Premium Item) gắn theo quán ăn.
    /// Đồng bộ từ API /api/shops (ShopItems nested trong ShopModel).
    /// </summary>
    public class ShopItemModel
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        [Indexed]
        public string ShopId { get; set; } = string.Empty;

        public bool IsPremiumOnly { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? AudioUrl { get; set; }

        // Nhận dữ liệu JSON từ API (không lưu cột này trong bảng SQLite)
        [Ignore]
        public List<ShopItemTranslationModel> ShopItemTranslations { get; set; } = new();
    }
}
