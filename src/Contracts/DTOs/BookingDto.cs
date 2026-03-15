namespace Calendar.Contracts.DTOs;

public sealed record BookingDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public int RoomId { get; init; }
    public DateTime StartTimeUtc { get; init; }
    public DateTime EndTimeUtc { get; init; }
    public required string Status { get; init; }
}