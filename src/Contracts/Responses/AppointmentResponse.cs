namespace Calendar.Contracts.Responses;
/// <summary>
/// Ответ с информацией о личном событии
/// Используется в: Calendar API
/// </summary>
public sealed record AppointmentResponse(
    Guid Id,
    string Title,
    string Description,
    DateTimeOffset StartTime,
    TimeSpan Duration,
    string Location,
    string? RecurrentRule,
    Guid UserId,
    DateTime CreatedAt,
    int? RoomId = null);
