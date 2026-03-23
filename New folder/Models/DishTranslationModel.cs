using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FoodTour_WebAdmin.Api.Models;

public class DishTranslationModel
{
    public int Id { get; set; }
    public string DishId { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty; // e.g., "vi", "en", "ja"
    public string Name { get; set; } = string.Empty;

    // Navigation property
    [JsonIgnore]
    public DishModel Dish { get; set; } = null!;
}
