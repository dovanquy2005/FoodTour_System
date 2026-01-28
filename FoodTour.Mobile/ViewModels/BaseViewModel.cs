using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FoodTour.Mobile.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        // Thêm dấu ? vào đây
        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // Thêm dấu ? vào Action
        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "", Action? onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}