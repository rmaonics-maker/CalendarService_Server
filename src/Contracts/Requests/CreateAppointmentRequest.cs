namespace Calendar.Contracts.Requests;
/// <summary>
/// Запрос на создание личного события в календаре
/// Используется в: Calendar API
/// </summary>
public sealed record CreateAppointmentRequest(
    string Title,
    string Description,
    DateTimeOffset StartTime,
    TimeSpan Duration,
    string Location,
    string? RecurrentRule,
    Guid UserId // Берётся из JWT токена
    );
