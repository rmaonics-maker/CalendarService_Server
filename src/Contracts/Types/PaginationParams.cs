namespace Calendar.Contracts.Types;
/// <summary>
/// Параметры пагинации
/// </summary>
public sealed record PaginationParams(
    int Page = 1,
    int PageSize = 10
);