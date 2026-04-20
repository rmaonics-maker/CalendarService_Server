namespace Calendar.Contracts.Commands;
/// <summary>
/// Команда: вернуть баланс
/// Отправляется: Booking API → Users API (calendar_service)
/// Используется при удалении бронирования для возврата средств
/// </summary>
public sealed record RefundBalanceCommand(
    Guid UserId,
    decimal Amount,
    string Reason,
    Guid RelatedBookingId
);