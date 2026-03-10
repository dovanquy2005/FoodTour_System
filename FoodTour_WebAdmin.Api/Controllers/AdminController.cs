using Microsoft.AspNetCore.Mvc;
using FoodTour_WebAdmin.Api.DTOs;
using FoodTour_WebAdmin.Api.Services;

namespace FoodTour_WebAdmin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly ManageFoodTourService _foodService;

    public AdminController(ManageFoodTourService foodService)
    {
        _foodService = foodService;
    }

    [HttpPost("shops")]
    public async Task<IActionResult> CreateShop([FromBody] CreateShopRequest request)
    {
        var result = await _foodService.CreateShopWithTranslationAsync(request);
        return Ok(result);
    }

    [HttpPost("dishes")]
    public async Task<IActionResult> CreateDish([FromBody] CreateDishRequest request)
    {
        var result = await _foodService.CreateDishWithTranslationAsync(request);
        return Ok(result);
    }
}
