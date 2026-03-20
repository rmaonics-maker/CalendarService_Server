namespace Calendar.Contracts.Events;
/// <summary>
/// Событие: личное событие удалено
/// /// Публикуется: Calendar API
/// Потребляется: Booking API (для удаления связанных бронирований), NotificationService
/// </summary>
public sealed record AppointmentDeletedEvent(
    Guid AppointmentId,
    Guid UserId,
    DateTime OccurredAt);