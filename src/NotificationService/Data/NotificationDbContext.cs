using Microsoft.EntityFrameworkCore;
using MassTransit;
using NotificationService.Entities;

namespace NotificationService.Data;

public class NotificationDbContext(DbContextOptions<NotificationDbContext> options) 
: DbContext(options)
{
    public DbSet<EmailLog> EmailLogs {get;set;}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<EmailLog>()
        .HasIndex(e=>new {e.UserId, e.EmailType});
    }
}