using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Calendar.Booking.API.Models;

public sealed class BookingModel
{
    public Guid Id { get; init; }
    public string? Title { get; set; }
    public Guid UserId { get; set; }
    public Guid? AppointmentId { get; init; }
    public int RoomId { get; set; }
    public NpgsqlRange<DateTime> Period { get; set; }
    public DateTime CreatedAtUtc { get; init; }
    public uint Version { get; init; }
}

public sealed class BookingConfiguration : IEntityTypeConfiguration<BookingModel>
{
    public void Configure(EntityTypeBuilder<BookingModel> builder)
    {
        builder.ToTable("bookings");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title).IsRequired().HasMaxLength(200);
        builder.Property(b => b.UserId).IsRequired();
        builder.Property(b => b.RoomId).IsRequired();
        builder.Property(b => b.Period).IsRequired();
        builder.Property(b => b.CreatedAtUtc).IsRequired();
        builder.Property(b => b.Version).IsRowVersion();

        // GiST индекс для range запросов
        builder.HasIndex(b => b.Period)
            .HasMethod("gist")
            .HasDatabaseName("ix_bookings_period");
    }
}