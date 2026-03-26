using SQLite;

namespace FoodTour.Mobile.Models
{
    public class DishTranslationModel
    {
        [PrimaryKey]
        public int Id { get; set; } // Server-assigned PK — enables proper InsertOrReplace upsert

        [Indexed]
        public string DishId { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = string.Empty; // e.g., "vi", "en"
        
        public string Name { get; set; } = string.Empty;
    }
}
