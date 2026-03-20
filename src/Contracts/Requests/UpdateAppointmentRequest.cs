namespace Calendar.Contracts.Requests;
/// <summary>
/// Запрос на обновление личного события
/// Используется в: Calendar API
/// </summary>
public sealed record UpdateAppointmentRequest(
    string Title,
    string Description,
    TimeSpan Duration);