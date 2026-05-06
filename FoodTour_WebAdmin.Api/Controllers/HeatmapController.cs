using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodTour_WebAdmin.Api.Data;

namespace FoodTour_WebAdmin.Api.Controllers;

[ApiController]
[Route("api/heatmap")]
public class HeatmapController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public HeatmapController(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// GET /api/heatmap?mode=unique&hours=24
    /// mode=unique  → weight = distinct IPAddress per shop
    /// mode=total   → weight = total trial count per shop
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetHeatmapData(
        [FromQuery] string mode = "unique",
        [FromQuery] int hours = 24)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var since = DateTime.UtcNow.AddHours(-hours);

        // Server-side only — no data loaded into memory until ToListAsync
        var data = await db.AudioActivityLogs
            .Where(t => t.PlayedAt >= since
                     && t.ShopId != null
                     && t.ShopId != string.Empty)
            .Join(db.Shops,
                  t => t.ShopId,
                  s => s.Id,
                  (t, s) => new
                  {
                      t.DeviceId,
                      s.Latitude,
                      s.Longitude,
                      ShopId = s.Id
                  })
            .GroupBy(x => new { x.ShopId, x.Latitude, x.Longitude })
            .Select(g => new
            {
                lat = g.Key.Latitude,
                lng = g.Key.Longitude,
                weight = mode == "unique"
                    ? g.Select(x => x.DeviceId).Distinct().Count()
                    : g.Count()
            })
            .Where(x => x.lat != 0 && x.lng != 0 && x.weight > 0)
            .OrderByDescending(x => x.weight)
            .ToListAsync();

        return Ok(data);
    }
}