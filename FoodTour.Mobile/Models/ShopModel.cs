using Microsoft.Maui.Devices.Sensors;
using SQLite;

namespace FoodTour.Mobile.Models
{
    public class ShopModel
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsVisited { get; set; }
        public double Rating { get; set; }

        [Ignore]
        public Location Location => new Location(Latitude, Longitude);
    }
}