using SQLite;

namespace FoodTour.Mobile.Models
{
    public class DishModel
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        [Indexed]
        public string ShopId { get; set; } = string.Empty;

        public double Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;

        // For API Deserialization and View Binding
        [Ignore]
        public List<DishTranslationModel> DishTranslations { get; set; } = new();

        [Ignore]
        public string Name { get; set; } = string.Empty;
    }
}