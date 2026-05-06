using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FoodTour.Mobile.Models;

namespace FoodTour.Mobile.Services;

public class LocationTrackingService
{
    // Dùng Geolocation.Default (static) thay vì inject IGeolocation để tránh crash DI
    // Dùng App.DeviceId thay vì inject IHardwareIdService để hoạt động trên mọi platform
    private readonly HttpClient _httpClient;
    
    private readonly List<MovementPoint> _pointBuffer = new();
    private readonly int _batchSize = 10;
    private readonly TimeSpan _trackingInterval = TimeSpan.FromSeconds(30);
    private CancellationTokenSource? _cts;

    public LocationTrackingService()
    {
        _httpClient = new HttpClient();
    }

    public void StartTracking()
    {
        if (_cts != null) return;
        _cts = new CancellationTokenSource();
        Task.Run(() => TrackingLoop(_cts.Token));
    }

    public void StopTracking()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        
        // Cố gắng gửi nốt buffer còn lại
        if (_pointBuffer.Count > 0)
        {
            _ = SendBatchAsync();
        }
    }

    private async Task TrackingLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                // Kiểm tra quyền trước khi lấy vị trí
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                    {
                        Debug.WriteLine("[LocationTracking] Không có quyền GPS, dừng tracking.");
                        break;
                    }
                }

                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                // Dùng Geolocation.Default thay vì inject để tránh crash khi DI không tìm thấy IGeolocation
                var location = await Geolocation.Default.GetLocationAsync(request, token);

                if (location != null)
                {
                    _pointBuffer.Add(new MovementPoint
                    {
                        Latitude = location.Latitude,
                        Longitude = location.Longitude,
                        Speed = location.Speed,
                        Timestamp = DateTime.UtcNow
                    });

                    if (_pointBuffer.Count >= _batchSize)
                    {
                        await SendBatchAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi lấy vị trí: {ex.Message}");
            }

            try
            {
                await Task.Delay(_trackingInterval, token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task SendBatchAsync()
    {
        if (_pointBuffer.Count == 0) return;

        var pointsToSend = new List<MovementPoint>(_pointBuffer);
        _pointBuffer.Clear();

        try
        {
            // Lấy DeviceId từ App (đã được set khi App khởi động)
            var deviceId = App.DeviceId;
            if (string.IsNullOrEmpty(deviceId)) return;

            var payload = new RecordMovementRequest
            {
                DeviceId = deviceId,
                Points = pointsToSend
            };

            var url = $"{AppConfig.ApiBaseUrl}/api/movement/record";
            var response = await _httpClient.PostAsJsonAsync(url, payload);

            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"Gửi MovementLog thất bại: {response.StatusCode}");
                // Nếu muốn, có thể thêm lại vào buffer ở đây nếu gửi lỗi
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Lỗi gửi MovementLog: {ex.Message}");
        }
    }
}
