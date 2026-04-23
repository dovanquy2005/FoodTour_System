using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodTour_WebAdmin.Api.Data;
using FoodTour_WebAdmin.Api.Models;
using FoodTour_WebAdmin.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace FoodTour_WebAdmin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AudioLogsController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public AudioLogsController(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    [HttpPost("record")]
    [AllowAnonymous]
    public async Task<IActionResult> RecordLog([FromBody] RecordAudioLogRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        using var db = await _dbFactory.CreateDbContextAsync();

        // Validate ShopId exists
        var shopExists = await db.Shops.AnyAsync(s => s.Id == request.ShopId);
        if (!shopExists)
            return BadRequest(new { message = "ShopId không hợp lệ" });

        // Validate ShopItemId if provided
        if (request.ShopItemId.HasValue)
        {
            var itemExists = await db.ShopItems.AnyAsync(i => i.Id == request.ShopItemId.Value && i.ShopId == request.ShopId);
            if (!itemExists)
                return BadRequest(new { message = "ShopItemId không hợp lệ" });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var log = new AudioActivityLog
        {
            DeviceId = request.DeviceId,
            ShopId = request.ShopId,
            ShopItemId = request.ShopItemId,
            LanguageCode = request.LanguageCode,
            Source = request.Source,
            PlayedAt = DateTime.UtcNow,
            IPAddress = ipAddress,
            UserAgent = userAgent,
            BrowserFingerprint = request.BrowserFingerprint
        };

        db.AudioActivityLogs.Add(log);
        await db.SaveChangesAsync();

        return Ok(new { success = true });
    }
}
