namespace Calendar.Contracts.Dtos;
/// <summary>
/// DTO для профиля пользователя
/// Используется для передачи между сервисами
/// </summary>
public sealed record UserProfileDto(
    Guid Id,
    string Name,
    string Email,
    string? Phone,
    string? AvatarUrl,
    decimal Balance,
    DateTimeOffset CreatedAt,
    DateTime? LastLoginAt
);