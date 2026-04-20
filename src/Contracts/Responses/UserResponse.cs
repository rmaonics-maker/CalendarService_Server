namespace Calendar.Contracts.Responses;
/// <summary>
/// Ответ с информацией о профиле пользователя
/// Используется в: calendar_service
/// </summary>
public record UserResponse(
    Guid Id,
    string Name,
    string Email);