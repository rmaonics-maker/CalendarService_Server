namespace Calendar.Booking.API.Options;

public sealed class DistributedLockOptions
{
    public const string SectionName = "DistributedLock";
    
    public TimeSpan LockExpiry { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan LockWait { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan LockRetry { get; set; } = TimeSpan.FromMilliseconds(200);
}