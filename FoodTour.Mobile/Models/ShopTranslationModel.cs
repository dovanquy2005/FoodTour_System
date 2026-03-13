using SQLite;

namespace FoodTour.Mobile.Models
{
    public class ShopTranslationModel
    {
        [PrimaryKey, AutoIncrement]
        public int LocalId { get; set; } // SQLite needs its own primary key for local storage if we don't want to use compound keys

        [Indexed]
        public string ShopId { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = string.Empty; // e.g., "vi", "en"
        
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
