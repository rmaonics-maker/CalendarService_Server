using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Calendar_Service.Models;

public class Booking
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public NpgsqlRange<DateTime> Period { get; set; }
    public int RoomId { get; set; }
    public Guid UserId { get; set; } // Внешний ключ
    public User? User { get; set; }   // Навигационное свойство (может быть null при ленивой загрузке)
    public uint Version { get; set; }
}

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");
        builder.HasKey(b => b.Id);
        
        // Маппинг диапазона
        // tsrange - Timestamp Range (без часового пояса, храним UTC)
        // tstzrange - Timestamp Tumb zone Range (с часовым поясом)
        builder.Property(b => b.Period)
            .HasColumnType("tsrange")
            .IsRequired();
        
        // ИНДЕКСЫ
        
        // Создаем GiST индекс (Generalized Search Tree).
        // Обычный B-Tree индекс (как в словаре) не умеет искать пересечения диапазонов.
        // GiST - это R-Tree (как коллайдеры в Unity), он умеет быстро находить "геометрические" пересечения во времени.
        builder.HasIndex(x => x.Period)
            .HasMethod("gist");

        // Оптимистичная блокировка через системное поле xmin
        // Это гарантирует, что EF выбросит исключение, если кто-то изменил запись пока мы думали.
        builder.Property(x => x.Version)
            .IsRowVersion();

        builder.HasOne(b => b.User)
        .WithMany(u => u.Bookings)
        .HasForeignKey(b => b.UserId)
        .OnDelete(DeleteBehavior.Cascade); // Если удалим юзера, удалятся и брони
    }
}