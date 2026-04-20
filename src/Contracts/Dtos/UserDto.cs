namespace Calendar.Contracts.Dtos;
/// <summary>
/// Упрощённый DTO для пользователя
/// Используется в других сервисах для минимальной информации
/// </summary>
public sealed record UserDto(
    Guid Id,
    string Name,
    string Email
);