namespace Calendar.Contracts.Events;

public sealed record UserRegisteredEvent(string UserEmail, Guid UserId);
