using Calendar.Contracts.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Calendar_Service.Models;

public sealed class Appointment
{
    public Guid Id { get; init; }
    public required string Title { get; init; } 
    public required string Description { get; init; } 
    public TimeSlot Slot { get; init; } 
    public required string Location { get; init; } 
    public string? RecurrentRule { get; init; }
    public required string DateTimeZoneId { get; init; }
    public RecurrencePattern? Recurrence { get; init; }
    public Guid UserId { get; init; }
    public DateTimeOffset CreatedAt { get; set; }
    public uint Version { get; init; }
}

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).IsRequired().HasMaxLength(1000);
        
        builder.Property(e => e.Location).HasMaxLength(200).IsRequired();
        builder.Property(e => e.RecurrentRule).HasMaxLength(200);
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.Version).IsRowVersion();

        builder.HasIndex(e => e.UserId);

        builder.ComplexProperty(e => e.Slot, slotBuilder =>
        {
            slotBuilder.Property(s => s.Start)
                .HasColumnName("start_at")
                .IsRequired();

            slotBuilder.Property(s => s.End)
                .HasColumnName("end_at")
                .IsRequired();
        });

        builder.ComplexProperty(e => e.Recurrence, recurrenceBuilder =>
        {
            recurrenceBuilder.Property(r => r.RRule)
                .HasColumnName("recurrent_rule")
                .HasMaxLength(200);

            recurrenceBuilder.Property(r => r.Until)
                .HasColumnName("recurrent_until");

            recurrenceBuilder.Property(r => r.Count)
                .HasColumnName("recurrent_count");
        });

        builder.Property(e => e.DateTimeZoneId)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}