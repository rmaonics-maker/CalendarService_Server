namespace Calendar.Contracts.Responses;
/// <summary>
/// Результат операции
/// </summary>
public sealed record OperationResult(
    bool IsSuccess,
    string? Message = null,
    object? Data = null
);

/// <summary>
/// Результат операции с данными
/// </summary>
public sealed record OperationResult<T>(
    bool IsSuccess,
    T? Data = default,
    string? Message = null
) where T : class;