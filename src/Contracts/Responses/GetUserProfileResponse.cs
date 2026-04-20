using Calendar.Contracts.Dtos;

namespace Calendar.Contracts.Responses;
/// <summary>
/// Ответ на запрос профиля пользователя
/// </summary>
public sealed record GetUserProfileResponse(
    UserProfileDto? UserProfile
);