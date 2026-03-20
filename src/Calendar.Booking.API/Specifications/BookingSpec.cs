using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Calendar.Booking.API.Models;

namespace Calendar.Booking.API.Specifications;

public static class BookingSpec
{
    public static IQueryable<BookingModel> InPeriod(this IQueryable<BookingModel> query, DateTimeOffset start, DateTimeOffset end)
    {
        var startUtc = DateTime.SpecifyKind(start.UtcDateTime, DateTimeKind.Unspecified);
        var endUtc = DateTime.SpecifyKind(end.UtcDateTime, DateTimeKind.Unspecified);

        var range = new NpgsqlRange<DateTime>(startUtc, endUtc);

        return query.Where(b => b.Period.Overlaps(range));
    }

    public static IQueryable<BookingModel> ForRoom(this IQueryable<BookingModel> query, int roomId)
    {
        return query.Where(b => b.RoomId == roomId);
    }
}
