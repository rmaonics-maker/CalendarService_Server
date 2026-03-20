namespace Calendar.Contracts.Requests;
/// <summary>
/// Запрос на создание пользователя
/// Используется в: Calendar.Users.API (calendar_service) (внутренний, вызывается по событию UserRegisteredEvent)
/// </summary>
public sealed record CreateUserRequest(
    string Name, 
    string Email, 
    decimal InitialBalance=0);
