namespace Calendar.Contracts.Dtos;

/// <summary>
/// DTO для переговорной комнаты
/// Используется в: Calendar.Booking.API
/// </summary>
public sealed record RoomDto(
    int Id,
    string Name,
    string Description,
    int Capacity,
    decimal HourlyRate,
    bool IsAvailable
);