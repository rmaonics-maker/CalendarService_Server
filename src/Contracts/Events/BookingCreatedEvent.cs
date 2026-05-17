namespace Calendar.Contracts.Events;
/// <summary>
/// Событие: бронирование создано
/// Публикуется: Booking API
/// Потребляется: NotificationService
/// </summary>
public sealed record BookingCreatedEvent(
    Guid BookingId,
    string? Title,
    Guid UserId,
    int RoomId,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    Guid? AppointmentId,  // Опциональная ссылка на событие
    DateTimeOffset OccurredAt
);
