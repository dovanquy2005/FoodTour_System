using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FoodTour_WebAdmin.Api.Models;

public class Dish
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string ShopId { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public double Price { get; set; }

    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    // Navigation property
    [ForeignKey(nameof(ShopId))]
    [JsonIgnore]
    public Shop? Shop { get; set; }
}
