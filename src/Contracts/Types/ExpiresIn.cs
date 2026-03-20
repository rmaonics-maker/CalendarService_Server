namespace Calendar.Contracts.Types;
/// <summary>
/// Время жизни токена
/// </summary>
public sealed record ExpiresIn(
    int Seconds  // Обычно 3600 (1 час) для AccessToken
);