using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FoodTour_WebAdmin.Api.Models;

public class ShopTranslationModel
{
    public int Id { get; set; }
    public string ShopId { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty; // e.g., "vi", "en", "ja"
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty; // Audio Script

    // Navigation property
    [JsonIgnore]
    public ShopModel Shop { get; set; } = null!;
}
