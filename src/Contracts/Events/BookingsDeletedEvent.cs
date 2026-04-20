namespace Calendar.Contracts.Events;
/// <summary>
/// Событие: бронирования удалены
/// Публикуется: Booking API после выполнения команды
/// Потребляется: NotificationService
/// </summary>
public sealed record BookingsDeletedEvent(
    Guid AppointmentId,
    int Count
);