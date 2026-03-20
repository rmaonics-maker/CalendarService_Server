namespace Calendar.Contracts.Dtos;
/// <summary>
/// DTO для личного события
/// Используется во всех сервисах для передачи данных о событии
/// </summary>
public sealed record AppointmentDto(
    Guid Id,
    string Title,
    string Description,
    DateTimeOffset StartTime,
    TimeSpan Duration,
    string Location,
    string? RecurrentRule,
    Guid UserId,
    DateTime CreatedAt);