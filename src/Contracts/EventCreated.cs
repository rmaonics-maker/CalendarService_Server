namespace Calendar.Contracts;

public record EventCreated(
    Guid EventId,
    string Title,
    Guid UserId,
    string UserEmail, 
    DateTime OccurredAt
);
