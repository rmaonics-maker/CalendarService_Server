namespace Calendar.Contracts.Requests;
/// <summary>
/// Запрос на регистрацию нового пользователя
/// Используется в: Calendar.Identity.API (пока в auth)
/// После успешной регистрации публикуется UserRegisteredEvent
/// </summary>
public record RegisterRequest(
    string Username,
    string Email,
    string Password);