namespace Calendar.Contracts.Requests;
/// <summary>
/// Запрос на получение событий за период
/// Используется в: Calendar API
/// </summary>
public sealed record GetAppointmentOccurrencesRequest(
    DateTimeOffset From,
    DateTimeOffset To);