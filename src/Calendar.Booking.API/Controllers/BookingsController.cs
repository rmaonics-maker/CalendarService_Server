using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Calendar.Booking.API.Services;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Calendar.Booking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController(IBookingService service) : ControllerBase
{
    [HttpGet]
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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBooking(Guid id, CancellationToken token = default)
    {
        var userIdString = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                           ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized("Некорректный идентификатор пользователя в токене.");
        }

        var booking = await service.GetBookingAsync(id, token);

        if (booking is null)
        {
            return NotFound($"Бронирование с ID {id} не найдено.");
        }

        // Проверка: пользователь может видеть только свои бронирования
        if (booking.UserId != userId)
        {
            return Forbid("У вас нет прав для просмотра этого бронирования.");
        }

        return Ok(booking);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request, CancellationToken token = default)
    {
        var userIdString = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                           ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized("Некорректный идентификатор пользователя в токене.");
        }

        var bookingDto = new BookingDto
        (
            Id: (Guid)request.AppointmentId, // TODO доделать
            Title: request.Title,
            UserId: userId,
            RoomId: request.RoomId,
            StartTimeUtc: request.StartTimeUtc,
            EndTimeUtc: request.EndTimeUtc,
            CreatedAtUtc: DateTime.UtcNow,
            AppointmentId: request.AppointmentId
        );

        var createdBooking = await service.CreateBookingAsync(bookingDto, token);

        return CreatedAtAction(nameof(GetBooking), new { id = createdBooking.Id }, createdBooking);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateBooking(Guid id, [FromBody] CreateBookingRequest request, CancellationToken token = default)
    {
        var userIdString = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                           ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized("Некорректный идентификатор пользователя в токене.");
        }

        var existingBooking = await service.GetBookingAsync(id, token);
        if (existingBooking is null)
        {
            return NotFound($"Бронирование с ID {id} не найдено.");
        }

        // Проверка: пользователь может редактировать только свои бронирования
        if (existingBooking.UserId != userId)
        {
            return Forbid("У вас нет прав для редактирования этого бронирования.");
        }

        var bookingDto = new BookingDto
        (
            Id: id,
            Title: request.Title,
            UserId: userId,
            RoomId: request.RoomId,
            StartTimeUtc: request.StartTimeUtc,
            EndTimeUtc: request.EndTimeUtc,
            CreatedAtUtc: existingBooking.CreatedAtUtc,
            AppointmentId: request.AppointmentId
        );

        var success = await service.UpdateBookingAsync(id, bookingDto, token);

        if (!success)
        {
            return NotFound($"Бронирование с ID {id} не найдено.");
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteBooking(Guid id, CancellationToken token = default)
    {
        var userIdString = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                           ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized("Некорректный идентификатор пользователя в токене.");
        }

        var existingBooking = await service.GetBookingAsync(id, token);
        if (existingBooking is null)
        {
            return NotFound($"Бронирование с ID {id} не найдено.");
        }

        // Проверка: пользователь может удалять только свои бронирования
        if (existingBooking.UserId != userId)
        {
            return Forbid("У вас нет прав для удаления этого бронирования.");
        }

        var success = await service.DeleteBookingAsync(id, token);

        if (!success)
        {
            return NotFound($"Бронирование с ID {id} не найдено.");
        }

        return NoContent();
    }
}
