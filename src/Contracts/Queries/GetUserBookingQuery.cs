namespace Calendar.Contracts.Messages;
/// <summary>
/// Запрос на получение бронирований пользователя (интеграция)
/// </summary>
public sealed record GetUserBookingsQuery(Guid UserId);