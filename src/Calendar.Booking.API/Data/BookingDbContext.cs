using Calendar.Booking.API.Models;
using Microsoft.EntityFrameworkCore;
using MassTransit;

namespace Calendar.Booking.API.Data;

public class BookingDbContext(DbContextOptions<BookingDbContext> options) : DbContext(options)
{
    public DbSet<Models.BookingModel> Bookings { get; set; }
    public DbSet<RoomModel> Rooms { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingDbContext).Assembly);
        modelBuilder.HasPostgresExtension("btree_gist");
    }
}