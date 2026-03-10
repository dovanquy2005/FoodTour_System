namespace FoodTour_WebAdmin.Api.DTOs;

public class CreateDishRequest
{
    public string ShopId { get; set; } = string.Empty;
    public double Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    
    // Thông tin tiếng Việt (vi) gốc
    public string Name { get; set; } = string.Empty;
}
