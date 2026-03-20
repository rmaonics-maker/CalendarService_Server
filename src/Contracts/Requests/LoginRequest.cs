namespace Calendar.Contracts.Requests;
/// <summary>
/// Запрос на вход в систему
/// Используется в: Calendar.Identity.API (пока в auth)
/// Возвращает JWT токен
/// </summary>
public sealed record LoginRequest(
    string Email,
    string Password
);