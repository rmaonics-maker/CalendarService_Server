namespace Calendar.Contracts.Requests;
/// <summary>
/// Запрос на получение пользователя
/// Используется в: Calendar.Users.API (calendar_service)
/// </summary>
public record GetUserRequest(Guid UserId);