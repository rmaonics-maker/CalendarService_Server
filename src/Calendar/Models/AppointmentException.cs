using Calendar.Contracts.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodaTime;

namespace Calendar_Service.Models;

public sealed class AppointmentException
{
    public Guid Id { get; init; }
    public Guid MasterAppointmentId { get; init; }
    public required Instant OriginalOccurrenceDate { get; init; }
    public bool IsCancelled { get; init; }
    public TimeSlot? OverrideSlot { get; init; }
    public string? OverrideTitle { get; init; }
}

public sealed class AppointmentExceptionConfiguration : IEntityTypeConfiguration<AppointmentException>
{
    public void Configure(EntityTypeBuilder<AppointmentException> builder)
    {
        builder.ToTable("event_exceptions");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.MasterAppointmentId).IsRequired();
        builder.Property(e => e.OriginalOccurrenceDate).IsRequired();
        builder.Property(e => e.IsCancelled).HasDefaultValue(false);
        builder.Property(e => e.OverrideTitle).HasMaxLength(200);

        builder.ComplexProperty(e => e.OverrideSlot, slotBuilder =>
        {
            slotBuilder.IsRequired(false);
            slotBuilder.Property(s => s.Start).HasColumnName("override_start_at");
            slotBuilder.Property(s => s.End).HasColumnName("override_end_at");
        });

        builder.HasIndex(e => e.MasterAppointmentId);
        builder.HasIndex(e => new { e.MasterAppointmentId, e.OriginalOccurrenceDate }).IsUnique();

        builder.HasOne<Appointment>()
            .WithMany()
            .HasForeignKey(e => e.MasterAppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

