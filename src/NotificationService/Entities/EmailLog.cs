using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NotificationService.Entities;

public sealed class EmailLog
{
    public Guid Id {get;init;}
    public Guid UserId {get;init;}
    public string EmailAddress {get;init;}
    public string EmailType {get;init;}
    public DateTimeOffset SentAt {get;init;}
}

public sealed class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.ToTable("email_logs");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.EmailAddress).IsRequired().HasMaxLength(200);
        builder.Property(e => e.EmailType).IsRequired().HasMaxLength(200);
        builder.Property(e => e.SentAt).IsRequired();
    }
}