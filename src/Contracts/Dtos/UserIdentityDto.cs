namespace Calendar.Contracts.Dtos;
/// <summary>
/// Минимальная информация о пользователе для JWT claims
/// Используется в: Calendar.Identity.API (auth) при создании токена
/// </summary>
public sealed record UserIdentityDto(
    Guid UserId,
    string Email
);