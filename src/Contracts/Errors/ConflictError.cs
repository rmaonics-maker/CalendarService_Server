namespace Calendar.Contracts.Errors;
/// <summary>
/// Ошибка: конфликт (например, дубликат email)
/// </summary>
public sealed record ConflictError(
    string ConflictType,
    string Message
);