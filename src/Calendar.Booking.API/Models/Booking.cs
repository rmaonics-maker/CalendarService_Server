using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Calendar.Booking.API.Models;

public sealed class Booking
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public NpgsqlRange<DateTime> Period { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public uint Version { get; init; }
}

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        throw new NotImplementedException();
    }
}