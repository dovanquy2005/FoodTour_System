using CommunityToolkit.Mvvm.ComponentModel;

namespace FoodTour.Mobile.Models
{
    public class LanguageOption
    {
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string FlagIcon { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}
