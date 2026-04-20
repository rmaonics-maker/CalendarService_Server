namespace Calendar.Contracts.Errors;
/// <summary>
/// Ошибка валидации
/// Используется во всех сервисах
/// </summary>
public sealed record ValidationError(
    string Field,
    string Message
);