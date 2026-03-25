using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodTour_WebAdmin.Api.Data;
using FoodTour_WebAdmin.Api.Models;

namespace FoodTour_WebAdmin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShopsController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly FoodTour_WebAdmin.Api.Services.ManageFoodTourService _manageService;

    public ShopsController(IDbContextFactory<AppDbContext> dbFactory, FoodTour_WebAdmin.Api.Services.ManageFoodTourService manageService)
    {
        _dbFactory = dbFactory;
        _manageService = manageService;
    }

    // GET: api/shops
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShopModel>>> GetAll()
    {
        using var _db = await _dbFactory.CreateDbContextAsync();
        var shops = await _db.Shops
            .Include(s => s.ShopTranslations)
            .OrderByDescending(s => s.Rating)
            .ToListAsync();
        return Ok(shops);
    }

    // GET: api/shops/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<ShopModel>> GetById(string id)
    {
        using var _db = await _dbFactory.CreateDbContextAsync();
        var ShopModel = await _db.Shops
            .Include(s => s.ShopTranslations)
            .Include(s => s.Dishes)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (ShopModel is null)
            return NotFound(new { message = $"ShopModel with id '{id}' not found." });

        return Ok(ShopModel);
    }

    // POST: api/shops
    [HttpPost]
    public async Task<ActionResult<ShopModel>> Create([FromBody] FoodTour_WebAdmin.Api.DTOs.CreateShopRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var shop = await _manageService.CreateShopWithTranslationAsync(request);

        return CreatedAtAction(nameof(GetById), new { id = shop.Id }, shop);
    }

    // PUT: api/shops/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(string id, [FromBody] FoodTour_WebAdmin.Api.DTOs.CreateShopRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _manageService.UpdateShopWithTranslationAsync(id, request);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi cập nhật Shop: " + ex.Message });
        }
    }

    // DELETE: api/shops/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        using var _db = await _dbFactory.CreateDbContextAsync();
        var ShopModel = await _db.Shops.FindAsync(id);
        if (ShopModel is null)
            return NotFound(new { message = $"ShopModel with id '{id}' not found." });

        _db.Shops.Remove(ShopModel);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/shops/stats
    [HttpGet("stats")]
    public async Task<ActionResult> GetStats()
    {
        using var _db = await _dbFactory.CreateDbContextAsync();
        var totalShops = await _db.Shops.CountAsync();
        var totalDishes = await _db.Dishes.CountAsync();
        var avgRating = totalShops > 0 ? await _db.Shops.AverageAsync(s => s.Rating) : 0;
        var topShop = await _db.Shops.OrderByDescending(s => s.Rating).FirstOrDefaultAsync();

        return Ok(new
        {
            totalShops,
            totalDishes,
            averageRating = Math.Round(avgRating, 1),
            topShopName = topShop?.Name ?? "N/A"
        });
    }
}
