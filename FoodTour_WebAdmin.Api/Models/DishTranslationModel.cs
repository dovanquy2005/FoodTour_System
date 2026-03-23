using System.Text.Json.Serialization;

namespace FoodTour_WebAdmin.Api.Models;

public class DishTranslationModel
{
    public int Id { get; set; }
    public string DishId { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    [JsonIgnore]
    public DishModel Dish { get; set; } = null!;
}