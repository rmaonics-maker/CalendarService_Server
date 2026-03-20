namespace Calendar.Contracts.Errors;
/// <summary>
/// Ошибка: ресурс не найден
/// </summary>
public sealed record NotFoundError(
    string ResourceType,
    string ResourceId
);