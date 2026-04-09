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

    private void OnSliderDragCompleted(object sender, EventArgs e)
    {
        if (BindingContext is PlayerViewModel vm)
        {
            if (vm.SeekCommand.CanExecute(null))
            {
                vm.SeekCommand.Execute(null);
            }
        }
    }

    double _x, _y;
    bool _isHoveringDismiss = false;

    private void OnAvatarPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _x = MinimizedAvatar.TranslationX;
                _y = MinimizedAvatar.TranslationY;
                DismissZone.IsVisible = true;
                DismissZone.Opacity = 0;
                DismissZone.FadeTo(1, 250);
                break;
                
            case GestureStatus.Running:
                MinimizedAvatar.TranslationX = _x + e.TotalX;
                MinimizedAvatar.TranslationY = _y + e.TotalY;

                // Kéo sang phải > 80 (đến vùng giữa MH) và không kéo lên trên quá nhiều
                bool isHovering = e.TotalX > 80 && e.TotalY > -100;
                
                if (isHovering != _isHoveringDismiss)
                {
                    _isHoveringDismiss = isHovering;
                    if (_isHoveringDismiss)
                    {
                        DismissZone.ScaleTo(1.2, 150);
                        DismissZone.BackgroundColor = Color.FromArgb("#B71C1C"); // Đỏ đậm hơn
                    }
                    else
                    {
                        DismissZone.ScaleTo(1.0, 150);
                        DismissZone.BackgroundColor = Color.FromArgb("#E53935");
                    }
                }
                break;
                
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                var dismiss = _isHoveringDismiss;
                _isHoveringDismiss = false;
                
                DismissZone.FadeTo(0, 200).ContinueWith(t => 
                {
                    MainThread.BeginInvokeOnMainThread(() => DismissZone.IsVisible = false);
                });

                if (dismiss && BindingContext is PlayerViewModel vm)
                {
                    if (vm.CloseCommand.CanExecute(null))
                        vm.CloseCommand.Execute(null);
                }
                
                // Trả về vị trí ban đầu
                MinimizedAvatar.TranslateTo(0, 0, 300, Easing.SpringOut);
                break;
        }
    }
}
