namespace Calendar.Contracts.Requests;
/// <summary>
/// Запрос на обновление токена
/// Используется в: Calendar.Identity.API (пока в auth)
/// </summary>
public sealed record RefreshTokenRequest(string RefreshToken);