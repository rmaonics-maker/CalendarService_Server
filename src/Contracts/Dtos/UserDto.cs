namespace Calendar.Contracts.Dtos;
/// <summary>
/// Упрощённый DTO для пользователя
/// Используется в других сервисах для минимальной информации
/// </summary>
public sealed record UserDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}