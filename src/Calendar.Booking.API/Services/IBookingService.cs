using Calendar.Contracts.DTOs;

namespace Calendar.Booking.API.Services;

public interface IBookingService
{
    Task<IEnumerable<BookingDto>> GetBookingsAsync(Guid userId, CancellationToken token = default);
    Task<BookingDto> CreateBookingAsync(BookingDto bookingDto, CancellationToken token = default);
    Task<bool> DeleteBookingAsync(Guid id, CancellationToken token = default);
    Task<bool> UpdateBookingAsync(Guid id, BookingDto bookingDto, CancellationToken token = default);
}