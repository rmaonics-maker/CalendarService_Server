namespace Calendar.Contracts.Events;

/// <summary>
/// Событие: личное событие создано
/// Публикуется: Calendar API
/// Потребляется: NotificationService
/// </summary>
public sealed record AppointmentCreatedEvent(
    Guid AppointmentId,
    string Title,
    Guid UserId,
    string? UserEmail,
    DateTimeOffset OccurredAt);

