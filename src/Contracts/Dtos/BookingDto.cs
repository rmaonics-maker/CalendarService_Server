namespace Calendar.Contracts.Dtos;

/// <summary>
/// DTO для бронирования
/// UserId не включён - берётся из контекста JWT токена
/// Используется во всех сервисах для передачи данных о бронировании
/// </summary>
public sealed record BookingDto(
    Guid Id,
    string Title,
    int RoomId,
    //Guid UserId,
    DateTimeOffset StartTimeUtc,
    DateTimeOffset EndTimeUtc,
    DateTimeOffset CreatedAtUtc,
    Guid? AppointmentId  // Опциональная ссылка на событие
);