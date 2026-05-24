using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Calendar.Booking.API.Models;

public sealed class IdempotencyKeyModel
{
    public string Key { get; init; } = null!;
    public int StatusCode { get; init; }
    public string? ResponseBody { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}

public sealed class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKeyModel>
{
    public void Configure(EntityTypeBuilder<IdempotencyKeyModel> builder)
    {
        builder.ToTable("idempotency_keys");
        builder.HasKey(k => k.Key);
        builder.Property(i => i.ResponseBody).HasColumnType("jsonb");
    }
}