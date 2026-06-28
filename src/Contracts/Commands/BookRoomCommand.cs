namespace Calendar.Contracts.Commands;

/// <summary>
/// Команда: забронировать комнату для слота события (или его вхождения).
/// Отправляется: Calendar API → Booking API
/// Создаётся по одному сообщению на каждое вхождение повторяющегося события
/// (для неповторяющегося — одно сообщение).
/// </summary>
public sealed record BookRoomCommand(
    Guid AppointmentId,
    Guid UserId,
    int RoomId,
    string Title,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    string? OccurrenceKey);
