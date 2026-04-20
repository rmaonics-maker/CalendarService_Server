namespace Calendar.Contracts.Commands;
/// <summary>
/// Команда: создать профиль пользователя
/// Отправляется: Identity API → Users API (через MassTransit после регистрации) (пока не используется)
/// Используется при регистрации для создания профиля
/// </summary>
public sealed record CreateUserProfileCommand(
    Guid UserId,
    string Email,
    string Name,
    decimal InitialBalance = 0
);