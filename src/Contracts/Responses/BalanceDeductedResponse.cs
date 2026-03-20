namespace Calendar.Contracts.Responses;
/// <summary>
/// Ответ на списание баланса
/// </summary>
public sealed record BalanceDeductedResponse(
    Guid UserId,
    decimal OldBalance,
    decimal NewBalance,
    bool Success
);