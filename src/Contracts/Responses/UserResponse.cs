namespace Calendar.Contracts.Responses;
/// <summary>
/// Ответ с информацией о профиле пользователя
/// Используется в: calendar_service
/// </summary>
public record UserResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}