namespace Calendar.Booking.API.Services;

public class PostgreBookingService : IBookingService
{
    public Task<Booking> CreateBookingAsync(Booking booking, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteBookingAsync(Guid id, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<Booking>> GetBookingsAsync(Guid userId, CancellationToken token)
    {
        
    }

    public Task<bool> UpdateBookingAsync(Guid id, Booking booking, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}