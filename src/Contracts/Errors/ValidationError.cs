namespace Calendar.Contracts.Errors;
/// <summary>
/// Ошибка валидации
/// </summary>
public sealed record ValidationError(
    string Field,
    string Message
);