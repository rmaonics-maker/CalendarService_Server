namespace Calendar.Contracts.Requests;
/// <summary>
/// Запрос на удаление бронирования
/// Используется в: Calendar.Booking.API
/// </summary>
public sealed record DeleteBookingRequest(Guid BookingId);