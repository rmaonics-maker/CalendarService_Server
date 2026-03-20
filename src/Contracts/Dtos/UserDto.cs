namespace Calendar.Contracts.Dtos;
/// <summary>
/// DTO для пользователя
/// </summary>
public sealed record UserDto(
    Guid Id,
    string Name,
    string Email,
    decimal Balance,
    DateTime CreatedAt
);