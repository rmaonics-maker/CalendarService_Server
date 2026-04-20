namespace Calendar.Contracts.Requests;
/// <summary>
/// Запрос на обновление пользователя
/// Используется в: Calendar.Users.API (calendar_service)
/// </summary>
public record UpdateUserRequest(
    string Name,
    string Email);