namespace Calendar.Contracts;

public sealed class UserHistoryResponse
{
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public decimal Balance { get; set; }

    // Используем DTO, специфичный для истории.
    // EventCreated сюда не подходит (нет полей StartTime/EndTime).
    public List<HistoryEventDto> Events { get; set; } = new();

    // Nested DTO: Переделали в record для удобства логирования и сравнения.
    // Оставляем свойства (init), чтобы сохранить совместимость с текущим кодом маппинга.
    public sealed record HistoryEventDto
    {
        public Guid Id { get; init; }
        public required string Title { get; init; }
        public required string Location { get; init; }
        public DateTimeOffset StartTime { get; init; }
        public DateTimeOffset EndTime { get; init; }
    }
}
