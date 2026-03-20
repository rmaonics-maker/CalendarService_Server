namespace Calendar.Contracts.Responses;
/// <summary>
/// Ответ с информацией о пользователе
/// </summary>
public sealed record UserResponse(
    Guid Id,
    string Name,
    string Email,
    decimal Balance,
    DateTime CreatedAt
);
