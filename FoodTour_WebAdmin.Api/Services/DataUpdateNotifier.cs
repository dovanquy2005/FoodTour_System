namespace FoodTour_WebAdmin.Api.Services;

/// <summary>
/// Singleton event bus. DeviceManagementController fires NotifyDeviceUpdated()
/// after each /api/device/sync call. Dashboard subscribes — no SignalR.Client needed.
/// Blazor Server circuit handles UI push via InvokeAsync(StateHasChanged).
/// </summary>
public interface IDataUpdateNotifier
{
    event Action? OnDeviceUpdated;
    void NotifyDeviceUpdated();
}

public class DataUpdateNotifier : IDataUpdateNotifier
{
    public event Action? OnDeviceUpdated;
    public void NotifyDeviceUpdated() => OnDeviceUpdated?.Invoke();
}
