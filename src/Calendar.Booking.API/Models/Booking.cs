using NpgsqlTypes;

namespace Calendar.Booking.API.Models;

public sealed class Booking
{
    public Guid Id { get; set; }
    //public string Title { get; set; } = string.Empty;
    public NpgsqlRange<DateTime> Period { get; set; }
    public int RoomId { get; set; }
    public Guid UserId { get; set; } // Внешний ключ
    //public User? User { get; set; }   // Навигационное свойство (может быть null при ленивой загрузке)
    public uint Version { get; set; }
}