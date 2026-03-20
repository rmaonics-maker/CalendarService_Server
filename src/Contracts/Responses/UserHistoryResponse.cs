using Calendar.Contracts.Dtos;

namespace Calendar.Contracts.Responses;
/// <summary>
/// Ответ с историей пользователя (личные события + бронирования)
/// Используется в: Calendar API
/// </summary>
public sealed record UserHistoryResponse(
    Guid UserId,
    string Name,
    decimal Balance,
    List<HistoryItemDto> Appointments  // Объединённый список
);
