namespace Calendar.Contracts.Queries;
/// <summary>
/// Запрос на получение профиля пользователя (интеграция)
/// Отправляется: Calendar/Booking API → Users API
/// Используется для валидации существования пользователя
/// </summary>
public sealed record GetUserProfileQuery(
    Guid UserId
);