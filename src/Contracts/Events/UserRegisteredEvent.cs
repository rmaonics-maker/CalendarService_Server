namespace Calendar.Contracts.Events;
/// <summary>
/// Событие: пользователь зарегистрирован
/// Публикуется: Identity API (auth) после успешной регистрации
/// Потребляется: Users API (calendar_service) (для создания профиля), NotificationService
/// </summary>
public sealed record UserRegisteredEvent(
    Guid Id,
    string Name,
    string Email,
    DateTimeOffset OccurredAt
);
