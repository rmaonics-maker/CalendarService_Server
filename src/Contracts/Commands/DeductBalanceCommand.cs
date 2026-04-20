namespace Calendar.Contracts.Commands;
/// <summary>
/// Команда: списать баланс
/// Отправляется: Booking API → Users API (calendar_service)
/// Используется при создании бронирования для оплаты комнаты
/// </summary>
public sealed record DeductBalanceCommand(
    Guid UserId,
    decimal Amount,
    string Reason,
    Guid? RelatedBookingId
);