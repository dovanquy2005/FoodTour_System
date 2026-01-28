using System.Collections.ObjectModel;
using FoodTour.Mobile.Models;

namespace FoodTour.Mobile.ViewModels
{
    public class MapViewModel : BaseViewModel
    {
        // Danh sách các điểm POI để hiển thị lên bản đồ
        public ObservableCollection<PoiModel> Pois { get; set; } = new();

        public MapViewModel()
        {
            LoadPois();
        }

        private void LoadPois()
        {
            // Tạm thời tạo dữ liệu giả để test giao diện
            Pois.Add(new PoiModel { Name = "Ốc Oanh", Latitude = 10.762622, Longitude = 106.660172 });
            Pois.Add(new PoiModel { Name = "Lẩu Dê", Latitude = 10.762900, Longitude = 106.661000 });
        }
    }
}