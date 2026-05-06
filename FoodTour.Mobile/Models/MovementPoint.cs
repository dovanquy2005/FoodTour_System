using System;
using System.Collections.Generic;

namespace FoodTour.Mobile.Models;

public class MovementPoint
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Speed { get; set; }
    public DateTime Timestamp { get; set; }
}

public class RecordMovementRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public List<MovementPoint> Points { get; set; } = new();
}
