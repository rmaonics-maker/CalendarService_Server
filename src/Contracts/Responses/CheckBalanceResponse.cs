namespace Calendar.Contracts.Messages;
/// <summary>
/// Ответ на проверку баланса
/// </summary>
public sealed record CheckBalanceResponse(
    Guid UserId,
    decimal CurrentBalance,
    bool HasSufficientFunds,
    Guid BookingId
);