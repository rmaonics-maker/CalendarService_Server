namespace Calendar.Contracts.Responses;
/// <summary>
/// Ответ с проблемами валидации
/// Используется во всех HTTP API
/// </summary>
public sealed record ValidationProblemDetailsResponse(
    string Type,
    string Title,
    int Status,
    Dictionary<string, string[]> Errors
);