using Calendar.Contracts.Dtos;

namespace Calendar.Contracts.Responses;
/// <summary>
/// Ответ со списком бронирований
/// Используется в: Calendar.Booking.API
/// </summary>
public sealed record BookingListResponse(List<BookingDto> Bookings);