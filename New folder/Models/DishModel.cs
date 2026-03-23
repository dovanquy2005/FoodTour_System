using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FoodTour_WebAdmin.Api.Models;

public class DishModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ShopId { get; set; } = string.Empty;
    public double Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;

    // Navigation properties
    [JsonIgnore]
    public ShopModel Shop { get; set; } = null!;
    public ICollection<DishTranslationModel> DishTranslations { get; set; } = new List<DishTranslationModel>();

    // Backward compatibility for Blazor UI
    [NotMapped]
    public string Name { get => DishTranslations.FirstOrDefault(t => t.LanguageCode == "vi")?.Name ?? ""; set { var t = DishTranslations.FirstOrDefault(t => t.LanguageCode == "vi"); if (t != null) t.Name = value; } }
}
