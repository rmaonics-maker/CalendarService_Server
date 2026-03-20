using Calendar.Contracts.Types;

namespace Calendar.Contracts.Responses;
/// <summary>
/// Ответ на регистрацию или вход
/// Используется в: Calendar.Identity.API (пока в auth)
/// </summary>
public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    ExpiresIn ExpiresIn,
    Guid UserId
);