using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodTour_WebAdmin.Api.Models;

public class AudioActivityLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string DeviceId { get; set; } = string.Empty;

    [Required]
    public string ShopId { get; set; } = string.Empty;

    public Guid? ShopItemId { get; set; }

    [Required]
    public string LanguageCode { get; set; } = string.Empty;

    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;

    public string? IPAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? BrowserFingerprint { get; set; }

    /// <summary>Nguồn kích hoạt: Web, AppManual, AppAuto</summary>
    public string Source { get; set; } = "Web";

    // Navigation property
    public virtual ShopModel? Shop { get; set; }
}
