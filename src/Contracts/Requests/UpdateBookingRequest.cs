namespace Calendar.Contracts.Requests;
/// <summary>
/// Запрос на обновление бронирования
/// Используется в: Calendar.Booking.API
/// </summary>
public sealed record UpdateBookingRequest(
    string Title,
    int RoomId,
    DateTimeOffset StartTimeUtc,
    DateTimeOffset EndTimeUtc);