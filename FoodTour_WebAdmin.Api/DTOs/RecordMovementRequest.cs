using System;

namespace FoodTour_WebAdmin.Api.DTOs;

public class MovementPointRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Speed { get; set; }
    public DateTime Timestamp { get; set; }
}

public class RecordMovementRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public MovementPointRequest[] Points { get; set; } = Array.Empty<MovementPointRequest>();
}
