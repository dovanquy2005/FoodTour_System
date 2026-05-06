using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FoodTour_WebAdmin.Api.Data;
using Microsoft.Extensions.Logging;

namespace FoodTour_WebAdmin.Api.Services;

/// <summary>
/// Background Service chạy ngầm mỗi ngày 1 lần để dọn dẹp các log di chuyển cũ hơn 30 ngày.
/// Tránh làm phình to Database gây chậm hệ thống.
/// </summary>
public class MovementCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MovementCleanupService> _logger;
    private readonly int _daysToKeep = 30;

    public MovementCleanupService(IServiceProvider serviceProvider, ILogger<MovementCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MovementCleanupService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldLogsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong quá trình dọn dẹp MovementLogs.");
            }

            // Chạy dọn dẹp mỗi 24 giờ
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task CleanupOldLogsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();

        using var db = await dbFactory.CreateDbContextAsync(stoppingToken);

        var cutoffDate = DateTime.UtcNow.AddDays(-_daysToKeep);

        // Đếm số lượng cần xóa để log lại
        var oldLogsCount = await db.MovementLogs
            .Where(m => m.Timestamp < cutoffDate)
            .CountAsync(stoppingToken);

        if (oldLogsCount > 0)
        {
            _logger.LogInformation($"Tiến hành xóa {oldLogsCount} bản ghi MovementLog cũ hơn {cutoffDate}.");

            // Xóa trực tiếp bằng ExecuteDeleteAsync (chỉ hỗ trợ EF Core 7+)
            await db.MovementLogs
                .Where(m => m.Timestamp < cutoffDate)
                .ExecuteDeleteAsync(stoppingToken);

            _logger.LogInformation("Dọn dẹp MovementLog hoàn tất.");
        }
    }
}
