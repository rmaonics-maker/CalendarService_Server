using Calendar.Contracts.Dtos;

namespace Calendar.Contracts.Responses;
/// <summary>
/// Ответ на запрос бронирований пользователя
/// </summary>
public sealed record GetUserBookingsResponse(List<BookingDto> Bookings);