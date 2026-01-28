using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Calendar_Service.Models;

public sealed class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    // Техническое поле для защиты от Race Condition (Double Spending)
    public uint Version { get; set; }
}

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.ToTable(t => t.HasCheckConstraint("CK_User_Balance", "balance >= 0"));
        builder.Property(x => x.Balance).HasPrecision(18, 2);
        builder.Property(x => x.Version).IsRowVersion();
    }
}