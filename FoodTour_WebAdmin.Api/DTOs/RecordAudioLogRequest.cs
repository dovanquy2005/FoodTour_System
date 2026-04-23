using System;
using System.ComponentModel.DataAnnotations;

namespace FoodTour_WebAdmin.Api.DTOs;

public class RecordAudioLogRequest
{
    [Required]
    public string DeviceId { get; set; } = string.Empty;

    [Required]
    public string ShopId { get; set; } = string.Empty;

    public Guid? ShopItemId { get; set; }

    [Required]
    public string LanguageCode { get; set; } = string.Empty;

    public string? BrowserFingerprint { get; set; }

    /// <summary>Nguồn kích hoạt: Web, AppManual, AppAuto</summary>
    public string Source { get; set; } = "Web";
}
