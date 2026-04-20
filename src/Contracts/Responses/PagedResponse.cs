namespace Calendar.Contracts.Responses;
/// <summary>
/// Ответ с пагинацией
/// Используется во всех HTTP API
/// </summary>
public sealed record PagedResponse<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasNextPage,
    bool HasPreviousPage
);