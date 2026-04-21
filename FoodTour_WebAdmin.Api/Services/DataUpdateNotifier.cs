namespace FoodTour_WebAdmin.Api.Services;

/// <summary>
/// Singleton event bus. DeviceManagementController fires NotifyDeviceUpdated()
/// after each /api/device/sync call.
/// Blazor Server circuit handles UI push via InvokeAsync(StateHasChanged).
/// </summary>
public interface IDataUpdateNotifier
{
    event Action? OnDeviceUpdated;
    event Action? OnTrialRecorded;
    void NotifyDeviceUpdated();
    void NotifyTrialRecorded();
}

public class DataUpdateNotifier : IDataUpdateNotifier
{
    public event Action? OnDeviceUpdated;
    public event Action? OnTrialRecorded;

    public void NotifyDeviceUpdated() => OnDeviceUpdated?.Invoke();
    public void NotifyTrialRecorded() => OnTrialRecorded?.Invoke();
}