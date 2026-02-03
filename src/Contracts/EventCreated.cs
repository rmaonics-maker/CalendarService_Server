namespace Calendar.Contracts;

public record EventCreated(Guid EventId, string Title, string UserEmail, DateTime StartTime);
