namespace Calendar.Contracts.Events;
/// <summary>
/// Событие: баланс списан
/// Публикуется: Users API (auth)
/// Потребляется: NotificationService, Booking API (для подтверждения бронирования)
/// </summary>
public sealed record BalanceDeductedEvent(
    Guid UserId,
    decimal Amount,
    decimal NewBalance,
    string Reason,
    Guid? RelatedBookingId,  // Если списание связано с бронированием
    DateTime OccurredAt
);