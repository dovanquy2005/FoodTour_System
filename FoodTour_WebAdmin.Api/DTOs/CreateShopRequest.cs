namespace FoodTour_WebAdmin.Api.DTOs;

public class CreateShopRequest
{
    public string ImageUrl { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Rating { get; set; }
    public bool IsVisited { get; set; }

    // Thông tin tiếng Việt (vi) gốc
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
