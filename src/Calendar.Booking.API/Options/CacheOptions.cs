namespace Calendar.Booking.API.Options;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";
    public int BaseTltMinutes { get; set; } = 5;
    public int JitterMinutes { get; set; } = 1;
}