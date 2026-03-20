namespace Calendar.Contracts.Responses;
/// <summary>
/// Стандартный ответ об ошибке
/// </summary>
public sealed record ErrorResponse(
    string Type,
    string Title,
    int Status,
    string Detail,
    DateTime Timestamp
);