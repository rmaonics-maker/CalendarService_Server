namespace Calendar.Contracts.Responses;
/// <summary>
/// Ответ с информацией о профиле пользователя
/// Используется в: Calendar.Users.API (calendar_service) пока не используется
/// </summary>
public sealed record UserProfileResponse(
    Guid Id,
    string Name,
    string Email,
    string? Phone,
    string? AvatarUrl,
    decimal Balance,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);
