using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodTour_WebAdmin.Api.Models;

public class ShopModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ImageUrl { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Rating { get; set; }
    public bool IsVisited { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<ShopTranslationModel> ShopTranslations { get; set; } = new List<ShopTranslationModel>();
    public ICollection<DishModel> Dishes { get; set; } = new List<DishModel>();

    // Backward compatibility for Blazor UI
    [NotMapped]
    public string Name { get => ShopTranslations.FirstOrDefault(t => t.LanguageCode == "vi")?.Name ?? ""; set { var t = ShopTranslations.FirstOrDefault(t => t.LanguageCode == "vi"); if (t != null) t.Name = value; } }
    
    [NotMapped]
    public string Address { get => ShopTranslations.FirstOrDefault(t => t.LanguageCode == "vi")?.Address ?? ""; set { var t = ShopTranslations.FirstOrDefault(t => t.LanguageCode == "vi"); if (t != null) t.Address = value; } }
    
    [NotMapped]
    public string Description { get => ShopTranslations.FirstOrDefault(t => t.LanguageCode == "vi")?.Description ?? ""; set { var t = ShopTranslations.FirstOrDefault(t => t.LanguageCode == "vi"); if (t != null) t.Description = value; } }
}
