namespace Calendar.Contracts.Requests;
/// <summary>
/// Запрос на выход из системы
/// Используется в: Calendar.Identity.API (пока в auth)
/// </summary>
public sealed record LogoutRequest(string RefreshToken);