using Microsoft.Maui.Controls;
using FoodTour.Mobile.ViewModels;

namespace FoodTour.Mobile.Views;

public partial class LanguageSelectionPage : ContentPage
{
	public LanguageSelectionPage(SettingsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
