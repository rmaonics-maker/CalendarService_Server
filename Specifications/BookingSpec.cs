using Calendar_Service.Models;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace Calendar_Service.Specifications;

public static class BookingSpec
{
    public static IQueryable<Booking> InPeriod(this IQueryable<Booking> query, DateTimeOffset start, DateTimeOffset end)
    {
        var startUtc = DateTime.SpecifyKind(start.UtcDateTime, DateTimeKind.Unspecified);
        var endUtc = DateTime.SpecifyKind(end.UtcDateTime, DateTimeKind.Unspecified);

        var range = new NpgsqlRange<DateTime>(startUtc, endUtc);

        return query.Where(b => b.Period.Overlaps(range));
    }

    public static IQueryable<Booking> ForRoom(this IQueryable<Booking> query, int roomId)
    {
        return query.Where(b => b.RoomId == roomId);
    }
}
