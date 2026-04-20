namespace Calendar.Contracts.Errors;
/// <summary>
/// Ошибка: ресурс не найден
/// Используется во всех HTTP API
/// </summary>
public sealed record NotFoundError(
    string ResourceType,
    string ResourceId
);