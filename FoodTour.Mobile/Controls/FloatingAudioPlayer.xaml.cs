using FoodTour.Mobile.ViewModels;
using Microsoft.Maui.Controls;

namespace FoodTour.Mobile.Controls;

public partial class FloatingAudioPlayer : ContentView
{
    public FloatingAudioPlayer()
    {
        InitializeComponent();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        TrySetBindingContext();
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        TrySetBindingContext();
    }

    private void TrySetBindingContext()
    {
        // Always resolve PlayerViewModel from Shell's BindingContext
        // (AppShell sets BindingContext = PlayerViewModel in its constructor)
        if (Shell.Current?.BindingContext is PlayerViewModel playerVm)
        {
            BindingContext = playerVm;
        }
    }
}
