namespace Calendar.Contracts.Queries;
/// <summary>
/// Запрос на проверку баланса (интеграция)
/// Отправляется: Booking API → Users API
/// Используется при создании бронирования для проверки sufficient funds
/// </summary>
public sealed record CheckBalanceQuery(
    Guid UserId,
    decimal RequiredAmount,
    Guid? BookingId = null
);