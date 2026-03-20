namespace Calendar.Contracts.Events;
/// <summary>
/// Событие: бронирование обновлено
/// Публикуется: Booking API
/// Потребляется: NotificationService
/// </summary>
public sealed record BookingUpdatedEvent(
    Guid BookingId,
    string NewTitle,
    int NewRoomId,
    DateTime NewStartTimeUtc,
    DateTime NewEndTimeUtc,
    DateTime OccurredAt
);
