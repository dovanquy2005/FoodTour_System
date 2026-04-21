using SQLite;

namespace FoodTour.Mobile.Models
{
    public class ShopItemTranslationModel
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        [Indexed]
        public string ShopItemId { get; set; } = string.Empty;

        public string LanguageCode { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string? AudioUrl { get; set; }
        
        public bool IsAudioGenerated { get; set; }
    }
}
