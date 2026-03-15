namespace Calendar.Contracts.Requests;

public sealed record CreateBookingRequest(
    Guid RoomId,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc);