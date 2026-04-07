using Microsoft.AspNetCore.SignalR;

namespace FoodTour_WebAdmin.Api.Hubs;

/// <summary>
/// SignalR Hub dùng để đẩy thông báo cập nhật dữ liệu tới Mobile App theo thời gian thực.
/// Khi Admin thay đổi Shop (tên, audio, radius...) trên Web, Hub sẽ broadcast tín hiệu
/// "ReceiveUpdate" tới tất cả client đang kết nối, giúp app cập nhật ngay không cần restart.
/// </summary>
public class UpdateHub : Hub
{
    /// <summary>
    /// Ghi log khi một client Mobile kết nối thành công vào Hub.
    /// </summary>
    public override Task OnConnectedAsync()
    {
        Console.WriteLine($"[SignalR] Client kết nối: {Context.ConnectionId}");
        return base.OnConnectedAsync();
    }

    /// <summary>
    /// Ghi log khi client ngắt kết nối (tắt app, mất mạng...).
    /// </summary>
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[SignalR] Client ngắt kết nối: {Context.ConnectionId}");
        return base.OnDisconnectedAsync(exception);
    }
}
