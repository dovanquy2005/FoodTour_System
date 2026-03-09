using System.ComponentModel.DataAnnotations;

namespace FoodTour_WebAdmin.Api.Models;

public class Shop
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsVisited { get; set; }

    [Range(0, 5)]
    public double Rating { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ICollection<Dish> Dishes { get; set; } = new List<Dish>();
}
