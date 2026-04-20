namespace Calendar.Contracts.Queries;
/// <summary>
/// Запрос на валидацию существования пользователя (интеграция)
/// Отправляется: Calendar/Booking API → Users API
/// Используется для быстрой проверки существования пользователя
/// </summary>
public sealed record ValidateUserExistsQuery(
    Guid UserId
);