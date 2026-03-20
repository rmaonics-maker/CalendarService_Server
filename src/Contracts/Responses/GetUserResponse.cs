using Calendar.Contracts.Dtos;

namespace Calendar.Contracts.Messages;
/// <summary>
/// Ответ на запрос пользователя
/// </summary>
public sealed record GetUserResponse(UserDto? User);