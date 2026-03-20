namespace Calendar.Contracts.Responses;
/// <summary>
/// Ответ с информацией о бронировании
/// Используется в: Calendar.Booking.API
/// </summary>
public sealed record BookingResponse(
    Guid Id,
    string Title,
    Guid UserId,
    int RoomId,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    DateTime CreatedAtUtc,
    Guid? AppointmentId);