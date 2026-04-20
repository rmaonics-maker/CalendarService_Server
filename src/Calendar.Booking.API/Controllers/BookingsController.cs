using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AutoMapper;
using Calendar.Booking.API.Services;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Requests;
using Calendar.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Calendar.Booking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController(IBookingService service, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetBookings(CancellationToken token = default)
    {
        var userId = GetUserIdFromClaims();

        if (userId == Guid.Empty)
        {
            return Unauthorized("Некорректный идентификатор пользователя в токене.");
        }

        var bookings = await service.GetBookingsAsync(userId, token);

        return Ok(bookings);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBooking(Guid id, CancellationToken token = default)
    {
        var userId = GetUserIdFromClaims();

        if (userId == Guid.Empty)
        {
            return Unauthorized("Некорректный идентификатор пользователя в токене.");
        }

        var (userIdFromDatabase, booking) = await service.GetBookingAsync(id, token);

        if (booking is null)
        {
            return NotFound($"Бронирование с ID {id} не найдено.");
        }

        // Проверка: пользователь может видеть только свои бронирования
        if (userIdFromDatabase != userId)
        {
            return Forbid("У вас нет прав для просмотра этого бронирования.");
        }

        return Ok(booking);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request, CancellationToken token = default)
    {
        var userId = GetUserIdFromClaims();

        if (userId == Guid.Empty)
        {
            return Unauthorized("Некорректный идентификатор пользователя в токене.");
        }

        var bookingDto = mapper.Map<BookingDto>(request);
        bookingDto = bookingDto with {CreatedAtUtc = DateTime.UtcNow};

        var createdBooking = await service.CreateBookingAsync(userId, bookingDto, token);

        return CreatedAtAction(nameof(GetBooking), new { id = createdBooking.Id }, mapper.Map<BookingResponse>(createdBooking));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateBooking(Guid id, [FromBody] CreateBookingRequest request, CancellationToken token = default)
    {
        var userId = GetUserIdFromClaims();

        if (userId == Guid.Empty)
        {
            return Unauthorized("Некорректный идентификатор пользователя в токене.");
        }

        var (userIdFromDatabase, existingBooking) = await service.GetBookingAsync(id, token);
        
        if (existingBooking is null)
        {
            return NotFound($"Бронирование с ID {id} не найдено.");
        }

        // Проверка: пользователь может редактировать только свои бронирования
        if (userIdFromDatabase != userId)
        { // TODO переделать Forbid
            return Forbid("У вас нет прав для редактирования этого бронирования.");
        }

        var bookingDto = mapper.Map<BookingDto>(request);
        

        var success = await service.UpdateBookingAsync(id, bookingDto, token);

        if (success is null)
        {
            return NotFound($"Бронирование с ID {id} не найдено.");
        }

        return CreatedAtAction(nameof(GetBooking), new { id = success.Id }, mapper.Map<BookingResponse>(success));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteBooking(Guid id, CancellationToken token = default)
    {
        var userId = GetUserIdFromClaims();

        if (userId == Guid.Empty)
        {
            return Unauthorized("Некорректный идентификатор пользователя в токене.");
        }

        var (userIdFromDatabase, existingBooking) = await service.GetBookingAsync(id, token);
        
        if (existingBooking is null)
        {
            return NotFound($"Бронирование с ID {id} не найдено.");
        }

        // Проверка: пользователь может редактировать только свои бронирования
        if (userIdFromDatabase != userId)
        {
            return Forbid("У вас нет прав для редактирования этого бронирования.");
        }

        var success = await service.DeleteBookingAsync(id, token);

        if (!success)
        {
            return NotFound($"Бронирование с ID {id} не найдено.");
        }

        return NoContent();
    }

    private Guid GetUserIdFromClaims()
    {
        var userIdString = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                           ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdString, out var userId) ? userId : Guid.Empty;
    }
}
