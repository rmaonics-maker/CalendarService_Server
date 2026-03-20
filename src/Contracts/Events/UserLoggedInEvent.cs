namespace Calendar.Contracts.Events;
/// <summary>
/// Событие: пользователь вошёл в систему
/// Публикуется: Identity API (auth) после успешного входа
/// Потребляется: Users API (calendar_service) (для обновления LastLoginAt), NotificationService
/// </summary>
public sealed record UserLoggedInEvent(
    Guid UserId,
    string Email,
    DateTime OccurredAt
);