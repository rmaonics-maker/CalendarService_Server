namespace Calendar.Contracts.Errors;
/// <summary>
/// Ошибка: конфликт (например, дубликат email)
/// Используется во всех HTTP API
/// </summary>
public sealed record ConflictError(
    string ConflictType,
    string Message
);