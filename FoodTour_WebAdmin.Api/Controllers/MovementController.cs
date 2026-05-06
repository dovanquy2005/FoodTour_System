using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodTour_WebAdmin.Api.Data;
using FoodTour_WebAdmin.Api.Models;
using FoodTour_WebAdmin.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;

namespace FoodTour_WebAdmin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MovementController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public MovementController(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    [HttpPost("record")]
    [AllowAnonymous]
    public async Task<IActionResult> RecordMovement([FromBody] RecordMovementRequest request)
    {
        if (string.IsNullOrEmpty(request.DeviceId) || request.Points == null || request.Points.Length == 0)
        {
            return BadRequest(new { message = "Dữ liệu không hợp lệ." });
        }

        using var db = await _dbFactory.CreateDbContextAsync();

        var logs = request.Points.Select(p => new MovementLog
        {
            DeviceId = request.DeviceId,
            Latitude = p.Latitude,
            Longitude = p.Longitude,
            Speed = p.Speed,
            Timestamp = p.Timestamp.ToUniversalTime()
        }).ToList();

        db.MovementLogs.AddRange(logs);
        await db.SaveChangesAsync();

        return Ok(new { success = true, count = logs.Count });
    }

    [HttpGet("history/{deviceId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHistory(string deviceId, [FromQuery] int days = 30)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        
        var cutoff = System.DateTime.UtcNow.AddDays(-days);

        var logs = await db.MovementLogs
            .Where(m => m.DeviceId == deviceId && m.Timestamp >= cutoff)
            .OrderBy(m => m.Timestamp)
            .Select(m => new { m.Latitude, m.Longitude, m.Speed, m.Timestamp })
            .ToListAsync();

        return Ok(logs);
    }
}
