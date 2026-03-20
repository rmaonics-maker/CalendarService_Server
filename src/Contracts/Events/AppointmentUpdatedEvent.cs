namespace Calendar.Contracts.Events;
/// <summary>
/// Событие: личное событие обновлено
/// Публикуется: Calendar API
/// Потребляется: NotificationService
/// </summary>
public record AppointmentUpdatedEvent(
    Guid AppointmentId,
    string NewTitle,
    DateTime OccurredAt);