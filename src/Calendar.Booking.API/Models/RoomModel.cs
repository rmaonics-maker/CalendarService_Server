using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Calendar.Booking.API.Models;

public sealed class RoomModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal HourlyRate { get; set; }
    public bool IsAvailable { get; set; }
}

public sealed class RoomConfiguration : IEntityTypeConfiguration<RoomModel>
{
    public void Configure(EntityTypeBuilder<RoomModel> builder)
    {
        builder.ToTable("rooms");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Capacity).HasDefaultValue(10);
        builder.Property(r => r.HourlyRate).HasPrecision(18, 2).HasDefaultValue(10.0m);
        builder.Property(r => r.IsAvailable).HasDefaultValue(true);

        // Seed данные
        builder.HasData(
            new RoomModel { Id = 1, Name = "Конференц-зал A", Capacity = 10, HourlyRate = 10.0m, IsAvailable = true },
            new RoomModel { Id = 2, Name = "Конференц-зал B", Capacity = 8, HourlyRate = 15.0m, IsAvailable = true },
            new RoomModel { Id = 3, Name = "Тренинг-рум", Capacity = 20, HourlyRate = 25.0m, IsAvailable = true }
        );
    }
}