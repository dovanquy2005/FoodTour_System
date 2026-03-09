using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodTour_WebAdmin.Api.Data;
using FoodTour_WebAdmin.Api.Models;

namespace FoodTour_WebAdmin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DishesController : ControllerBase
{
    private readonly AppDbContext _db;

    public DishesController(AppDbContext db) => _db = db;

    // GET: api/dishes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Dish>>> GetAll([FromQuery] string? shopId = null)
    {
        var query = _db.Dishes.AsQueryable();

        if (!string.IsNullOrEmpty(shopId))
            query = query.Where(d => d.ShopId == shopId);

        var dishes = await query.OrderBy(d => d.Name).ToListAsync();
        return Ok(dishes);
    }

    // GET: api/dishes/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Dish>> GetById(string id)
    {
        var dish = await _db.Dishes.FindAsync(id);

        if (dish is null)
            return NotFound(new { message = $"Dish with id '{id}' not found." });

        return Ok(dish);
    }

    // POST: api/dishes
    [HttpPost]
    public async Task<ActionResult<Dish>> Create([FromBody] Dish dish)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Verify shop exists
        var shopExists = await _db.Shops.AnyAsync(s => s.Id == dish.ShopId);
        if (!shopExists)
            return BadRequest(new { message = $"Shop with id '{dish.ShopId}' does not exist." });

        if (string.IsNullOrEmpty(dish.Id))
            dish.Id = Guid.NewGuid().ToString();

        _db.Dishes.Add(dish);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = dish.Id }, dish);
    }

    // PUT: api/dishes/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<Dish>> Update(string id, [FromBody] Dish dish)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existing = await _db.Dishes.FindAsync(id);
        if (existing is null)
            return NotFound(new { message = $"Dish with id '{id}' not found." });

        // Verify shop exists if ShopId changed
        if (existing.ShopId != dish.ShopId)
        {
            var shopExists = await _db.Shops.AnyAsync(s => s.Id == dish.ShopId);
            if (!shopExists)
                return BadRequest(new { message = $"Shop with id '{dish.ShopId}' does not exist." });
        }

        existing.ShopId = dish.ShopId;
        existing.Name = dish.Name;
        existing.Price = dish.Price;
        existing.ImageUrl = dish.ImageUrl;

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    // DELETE: api/dishes/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var dish = await _db.Dishes.FindAsync(id);
        if (dish is null)
            return NotFound(new { message = $"Dish with id '{id}' not found." });

        _db.Dishes.Remove(dish);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
