using Microsoft.Maui.Devices.Sensors;
using SQLite;

namespace FoodTour.Mobile.Models
{
    public class DishModel
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Indexed] // Đánh chỉ mục để tìm kiếm theo Shop nhanh hơn
        public string ShopId { get; set; } = string.Empty; // Khóa ngoại liên kết với ShopModel.Id

        public string Name { get; set; } = string.Empty;
        public double Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        
    }
}