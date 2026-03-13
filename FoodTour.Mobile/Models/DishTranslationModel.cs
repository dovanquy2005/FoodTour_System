using SQLite;

namespace FoodTour.Mobile.Models
{
    public class DishTranslationModel
    {
        [PrimaryKey, AutoIncrement]
        public int LocalId { get; set; }

        [Indexed]
        public string DishId { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = string.Empty; // e.g., "vi", "en"
        
        public string Name { get; set; } = string.Empty;
    }
}
