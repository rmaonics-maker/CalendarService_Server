namespace Calendar.Contracts.Types;
/// <summary>
/// Параметры пагинации
/// Используется во всех HTTP API
/// </summary>
public sealed record PaginationParams(
    int Page = 1,
    int PageSize = 10
);