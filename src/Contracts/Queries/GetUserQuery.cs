namespace Calendar.Contracts.Messages;
/// <summary>
/// Запрос на получение пользователя (интеграция)
/// </summary>
public sealed record GetUserQuery(
    Guid UserId
);