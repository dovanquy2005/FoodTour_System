namespace FoodTour_WebAdmin.Api.DTOs;

public class CreateShopItemRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPremiumOnly { get; set; } = true;
}
