using SQLite;

namespace FoodTour.Mobile.Models
{
    public class DishModel
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Indexed] // Đánh chỉ mục để tìm kiếm theo Shop nhanh hơn
        public string ShopId { get; set; } = string.Empty; // Khóa ngoại liên kết với ShopModel.Id

        public double Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;

        // For API Deserialization and View Binding
        [Ignore]
        public List<DishTranslationModel> DishTranslations { get; set; } = new();

        [Ignore]
        public string Name { get; set; } = string.Empty;
    }
}