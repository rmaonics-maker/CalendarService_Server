namespace Calendar.Contracts.Requests;
/// <summary>
/// Запрос на получение истории пользователя
/// Используется в: Calendar API (агрегирует данные из Calendar и Booking)
/// </summary>
public record GetUserHistoryRequest(
    Guid UserId,
    DateTimeOffset From,
    DateTimeOffset To);