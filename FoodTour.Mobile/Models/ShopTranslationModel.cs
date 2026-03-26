using SQLite;

namespace FoodTour.Mobile.Models
{
    public class ShopTranslationModel
    {
        [PrimaryKey]
        public int Id { get; set; } // Server-assigned PK — enables proper InsertOrReplace upsert

        [Indexed]
        public string ShopId { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = string.Empty; // e.g., "vi", "en"
        
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string? AudioUrl { get; set; }
        public bool IsAudioGenerated { get; set; }
    }
}
