namespace Calendar.Booking.API.Models;

public sealed class Room
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public bool IsAvailableForBooking { get; init; }
}