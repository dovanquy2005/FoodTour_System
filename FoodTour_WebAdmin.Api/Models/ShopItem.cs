using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FoodTour_WebAdmin.Api.Models;

/// <summary>
/// Nội dung độc quyền (Premium) gắn theo quán ăn.
/// </summary>
public class ShopItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ShopId { get; set; } = string.Empty; 
    public bool IsPremiumOnly { get; set; } = true;

    [JsonIgnore]
    [ForeignKey("ShopId")] 
    public ShopModel Shop { get; set; } = null!;

    // Quan hệ 1-Nhiều với bảng Dịch
    public ICollection<ShopItemTranslation> ShopItemTranslations { get; set; } = new List<ShopItemTranslation>();
}