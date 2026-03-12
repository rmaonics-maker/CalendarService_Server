using Microsoft.EntityFrameworkCore;
using MassTransit;
using Calendar.Auth.API.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Calendar.Auth.API.Data;

public class AuthDbContext(DbContextOptions<AuthDbContext> options) 
: IdentityDbContext<ApplicationUser>(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        modelBuilder.UseOpenIddict();
    }
}