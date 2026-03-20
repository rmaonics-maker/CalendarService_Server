namespace Calendar.Contracts.Events;
/// <summary>
/// Событие: баланс пополнен (возврат)
/// Публикуется: Users API (calendar_service)
/// Потребляется: NotificationService, Booking API
/// </summary>
public sealed record BalanceRefundedEvent(
    Guid UserId,
    decimal Amount,
    decimal NewBalance,
    string Reason,
    Guid? RelatedBookingId,  // Если возврат связан с бронированием
    DateTime OccurredAt
);