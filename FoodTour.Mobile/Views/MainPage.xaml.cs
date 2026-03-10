using System;
using Microsoft.Maui.Controls;
using FoodTour.Mobile.Services;

namespace FoodTour.Mobile.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly ILocalizationService _localizationService;

        /// <summary>
        /// Constructor demonstrating Dependency Injection.
        /// </summary>
        public MainPage(ILocalizationService localizationService)
        {
            InitializeComponent();
            _localizationService = localizationService;
        }

        private async void OnVietnameseClicked(object sender, EventArgs e)
        {
            if (_localizationService != null)
            {
                await _localizationService.ChangeLanguageAsync("vi");
            }
        }

        private async void OnEnglishClicked(object sender, EventArgs e)
        {
            if (_localizationService != null)
            {
                await _localizationService.ChangeLanguageAsync("en");
            }
        }

        private async void OnSpeakClicked(object sender, EventArgs e)
        {
            if (_localizationService != null)
            {
                await _localizationService.SpeakTextAsync("WelcomeMessage");
            }
        }
    }
}
