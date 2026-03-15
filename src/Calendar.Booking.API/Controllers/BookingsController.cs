using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Calendar.Booking.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Calendar.Booking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController(IBookingService service) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetBookings(CancellationToken token = default)
    {
        var userIdString = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                           ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized("Некорректный идентификатор пользователя в токене.");
        }

        var bookings = await service.GetBookingsAsync(userId, token);
        
        return Ok(bookings);
    }
}
