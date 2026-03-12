using Calendar.Booking.API.Models;

namespace Calendar.Booking.API.Services;

public interface IBookingService
{
    Task<IEnumerable<Booking>> GetBookingsAsync(Guid userId, CancellationToken token);
    Task<Booking> CreateBookingAsync(Booking booking, CancellationToken token);
    Task<bool> DeleteBookingAsync(Guid id, CancellationToken token);
    Task<bool> UpdateBookingAsync(Guid id, Booking booking, CancellationToken token);
}