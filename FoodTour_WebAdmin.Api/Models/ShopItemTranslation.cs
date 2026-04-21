using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FoodTour_WebAdmin.Api.Models;

/// <summary>
/// Nội dung đa ngôn ngữ và Audio sinh từ TTS của từng ShopItem (Premium)
/// </summary>
public class ShopItemTranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid ShopItemId { get; set; }

    public string LanguageCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? AudioUrl { get; set; }
    
    public bool IsAudioGenerated { get; set; } = false;

    // Navigation property
    [JsonIgnore]
    [ForeignKey("ShopItemId")]
    public ShopItem ShopItem { get; set; } = null!;
}
