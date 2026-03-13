using FoodTour.Mobile.Models;

namespace FoodTour.Mobile.Messages;

/// <summary>
/// Message sent when the user wants to navigate to a specific shop.
/// Handled by MapPage.
/// </summary>
public record RouteToShopMessage(ShopModel TargetShop);
