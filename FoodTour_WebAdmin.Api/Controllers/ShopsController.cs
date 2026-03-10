using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodTour_WebAdmin.Api.Data;
using FoodTour_WebAdmin.Api.Models;

namespace FoodTour_WebAdmin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShopsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ShopsController(AppDbContext db) => _db = db;

    // GET: api/shops
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShopModel>>> GetAll()
    {
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
    public async Task<ActionResult<ShopModel>> Create([FromBody] ShopModel ShopModel)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrEmpty(ShopModel.Id))
            ShopModel.Id = Guid.NewGuid().ToString();

        ShopModel.CreatedAt = DateTime.UtcNow;
        ShopModel.UpdatedAt = DateTime.UtcNow;

        _db.Shops.Add(ShopModel);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = ShopModel.Id }, ShopModel);
    }

    // PUT: api/shops/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<ShopModel>> Update(string id, [FromBody] ShopModel ShopModel)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existing = await _db.Shops.Include(s => s.ShopTranslations).FirstOrDefaultAsync(s => s.Id == id);
        if (existing is null)
            return NotFound(new { message = $"ShopModel with id '{id}' not found." });

        existing.Name = ShopModel.Name;
        existing.Address = ShopModel.Address;
        existing.Description = ShopModel.Description;
        existing.ImageUrl = ShopModel.ImageUrl;
        existing.Latitude = ShopModel.Latitude;
        existing.Longitude = ShopModel.Longitude;
        existing.IsVisited = ShopModel.IsVisited;
        existing.Rating = ShopModel.Rating;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    // DELETE: api/shops/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
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
