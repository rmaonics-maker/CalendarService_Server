namespace Calendar.Contracts.Responses;
/// <summary>
/// Ответ на проверку баланса
/// </summary>
public sealed record CheckBalanceResponse(
    Guid UserId,
    decimal CurrentBalance,
    bool HasSufficientFunds,
    Guid? BookingId
);