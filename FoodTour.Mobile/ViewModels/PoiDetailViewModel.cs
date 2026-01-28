using FoodTour.Mobile.Models;

namespace FoodTour.Mobile.ViewModels
{
    [QueryProperty(nameof(PoiId), "PoiId")]
    public class PoiDetailViewModel : BaseViewModel
    {
        private string? poiId;      // Thêm ?
        private PoiModel? currentPoi; // Thêm ?

        public string? PoiId
        {
            get => poiId;
            set
            {
                poiId = value;
                if (value != null) LoadPoiDetail(value);
            }
        }

        public PoiModel? CurrentPoi
        {
            get => currentPoi;
            set => SetProperty(ref currentPoi, value);
        }

        private void LoadPoiDetail(string id)
        {
            CurrentPoi = new PoiModel { Name = "Ốc Oanh (Chi tiết)", Description = "Quán ngon số 1..." };
        }
    }
}