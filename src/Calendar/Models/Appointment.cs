using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Calendar_Service.Models;

public sealed class Appointment
{
    public Guid Id { get; init; }
    public string Title { get; set; } 
    public string Description { get; init; } 
    public NpgsqlRange<DateTime> Period { get; init; } 
    public string Location { get; init; } 
    public string? RecurrentRule { get; init; }
    public Guid UserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public uint Version { get; set; }
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
        builder.Property(e => e.CreatedAtUtc).IsRequired();
        builder.Property(e => e.Version).IsRowVersion();

        builder.HasIndex(e => e.UserId);
        

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}