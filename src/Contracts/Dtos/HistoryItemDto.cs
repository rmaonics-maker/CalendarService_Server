using Calendar.Contracts.Types;

namespace Calendar.Contracts.Dtos;
/// <summary>
/// DTO для элемента истории (личное событие ИЛИ бронирование)
/// Используется в Calendar API для агрегации истории
/// </summary>
public sealed record HistoryItemDto(
    Guid Id,
    string Title,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string Location,
    ItemType Type,      // "personal" или "booking"
    int? RoomId       // null для личных событий
);