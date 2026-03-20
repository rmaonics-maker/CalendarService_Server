namespace Calendar.Contracts.Requests;
/// <summary>
/// Запрос на получение бронирований пользователя
/// UserId берётся из JWT токена
/// Используется в: Calendar.Booking.API
/// </summary>
public sealed record GetUserBookingRequest(
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null);