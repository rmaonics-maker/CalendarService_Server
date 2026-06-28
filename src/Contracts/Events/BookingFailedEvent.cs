namespace Calendar.Contracts.Events;

/// <summary>
/// Событие: не удалось забронировать комнату (занято / нет блокировки).
/// Публикуется: Booking API
/// Потребляется: NotificationService
/// </summary>
public sealed record BookingFailedEvent(
    Guid AppointmentId,
    int RoomId,
    Guid UserId,
    string Reason,
    DateTime OccurrenceStartTimeUtc,
    DateTimeOffset OccurredAt);
