using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Calendar.Booking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetTestBookings()
    {
        var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? "Неизвестный ID";

        return Ok(new
        {
            Message = "Победа! Токен успешно прошел валидацию.",
            UserId = userId,
            Data = new[]
            {
                new { Id = 1, Title = "Встреча с Архитектором", Date = DateTime.Now },
                new { Id = 2, Title = "Деплой в Production", Date = DateTime.Now.AddDays(1) }
            }
        });
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetBookings()
    {
        var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? "Неизвестный ID";

        var bookings = await conte
    }
}
