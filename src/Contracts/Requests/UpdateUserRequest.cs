namespace Calendar.Contracts.Requests;
/// <summary>
/// Запрос на обновление пользователя
/// Используется в: Calendar.Users.API
/// </summary>
public record UpdateUserRequest(
    string Name,
    string Email);