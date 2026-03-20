using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Calendar_Service.Models;

public sealed class CalendarEvent
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTimeOffset StartTime { get; init; } 
    public TimeSpan Duration { get; init; }
    public string Location { get; init; } = string.Empty;
    public string? RecurrentRule { get; init; }
    public Guid UserId { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public uint Version { get; init; }
}

public sealed class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.ToTable("events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title).IsRequired();
        builder.Property(e => e.Description).IsRequired();
        builder.Property(e => e.StartTime).IsRequired();
        builder.Property(e => e.Duration).IsRequired();
        builder.Property(e => e.Location).HasMaxLength(200).IsRequired();
        
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.Version).IsRowVersion();

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.StartTime);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}