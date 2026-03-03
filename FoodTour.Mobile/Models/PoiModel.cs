using Microsoft.Maui.Devices.Sensors;
using SQLite; // 👈 Thêm dòng này

namespace FoodTour.Mobile.Models
{
    public class PoiModel
    {
        [PrimaryKey] // 👈 Đánh dấu ID là khóa chính
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string AudioUrl { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsVisited { get; set; }
        
        public double Price { get; set; }
        public double Rating { get; set; }

        // Property này không lưu vào Database nên thêm [Ignore]
        [Ignore]
        public Location Location => new Location(Latitude, Longitude);
    }
}