namespace FoodTour.Mobile.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private bool isAutoPlay;

        public bool IsAutoPlay
        {
            get => isAutoPlay;
            set => SetProperty(ref isAutoPlay, value);
        }

        public SettingsViewModel()
        {
            IsAutoPlay = true; // Mặc định bật
        }
    }
}