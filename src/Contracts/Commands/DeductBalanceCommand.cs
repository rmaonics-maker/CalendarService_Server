namespace Calendar.Contracts.Commands;
/// <summary>
/// Команда: списать баланс
/// </summary>
public sealed record DeductBalanceCommand(
    Guid UserId,
    decimal Amount,
    string Reason
);