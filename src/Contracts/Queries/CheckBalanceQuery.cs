namespace Calendar.Contracts.Messages;
/// <summary>
/// Запрос на проверку баланса (интеграция)
/// </summary>
public sealed record CheckBalanceQuery(
    Guid UserId,
    decimal RequiredAmount,
    Guid BookingId
);