namespace Calendar.Contracts.Queries;
/// <summary>
/// Запрос на получение бронирований пользователя (интеграция)
/// Отправляется: Calendar API → Booking API
/// Используется для: GetUserHistoryRequest (агрегация истории)
/// </summary>
public sealed record GetUserBookingsQuery(
    Guid UserId,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null
);