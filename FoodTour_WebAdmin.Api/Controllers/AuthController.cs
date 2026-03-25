using Microsoft.AspNetCore.Mvc;
using FoodTour_WebAdmin.Api.Services;
using FoodTour_WebAdmin.Api.DTOs;

namespace FoodTour_WebAdmin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            return BadRequest(new { message = "Email và Password không được để trống." });

        var user = await _authService.CheckLogin(request.Email, request.Password);
        
        if (user == null)
            return Unauthorized(new { message = "Email hoặc mật khẩu không chính xác." });

        // Trả về thông tin cơ bản sau khi login thành công (nếu có JWT sẽ trả ở đây)
        return Ok(new
        {
            message = "Đăng nhập thành công",
            user = new
            {
                user.Id,
                user.Email,
                user.FullName,
                user.Role
            }
        });
    }
}
