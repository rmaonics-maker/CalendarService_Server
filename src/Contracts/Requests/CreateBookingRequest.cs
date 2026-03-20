namespace Calendar.Contracts.Requests;
/// <summary>
/// Запрос на создание бронирования комнаты
/// Используется в: Calendar.Booking.API
/// </summary>
public sealed record CreateBookingRequest(
    string Title,
    int RoomId,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    Guid? AppointmentId);