namespace FoodTour_WebAdmin.Api.DTOs;

public class TrialRequest
{
    public string Fingerprint { get; set; } = string.Empty;
    public string? ShopId { get; set; }
}