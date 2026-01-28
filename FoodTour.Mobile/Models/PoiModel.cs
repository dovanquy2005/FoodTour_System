using Microsoft.Maui.Devices.Sensors;

namespace FoodTour.Mobile.Models
{
    public class PoiModel
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; } // 👈 Đã thêm cái này để Pin hiện địa chỉ
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? AudioUrl { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsVisited { get; set; }

        // Property phụ giúp Map hiểu toạ độ
        public Location Location => new Location(Latitude, Longitude);
    }
}