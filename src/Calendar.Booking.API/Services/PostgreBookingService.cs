using Calendar.Booking.API.Services;
using Calendar.Contracts.DTOs;

namespace Booking.API.Services;

public class PostgreBookingService : IBookingService
{
    public Task<IEnumerable<BookingDto>> GetBookingsAsync(Guid userId, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<BookingDto> CreateBookingAsync(BookingDto bookingDto, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteBookingAsync(Guid id, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateBookingAsync(Guid id, BookingDto bookingDto, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}