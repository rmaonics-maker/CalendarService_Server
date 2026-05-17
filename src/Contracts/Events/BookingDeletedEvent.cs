namespace Calendar.Contracts.Events;
/// <summary>
/// Событие: бронирование удалено
/// Публикуется: Booking API
/// Потребляется: NotificationService
/// </summary>
public sealed record BookingDeletedEvent(
    Guid BookingId,
    Guid UserId,
    Guid? AppointmentId,  // Опциональная ссылка на событие
    decimal RefundAmount,
    DateTimeOffset OccurredAt
);
