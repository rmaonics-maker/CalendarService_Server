using Calendar.Booking.API.Models;
using Calendar.Contracts.Dtos;

namespace Calendar.Booking.API.Services;

public interface IBookingService
{
    Task<IEnumerable<BookingDto>> GetBookingsAsync(Guid userId, CancellationToken token = default);
    Task<(Guid?, BookingDto?)> GetBookingAsync(Guid id, CancellationToken token = default);
    Task<BookingDto> CreateBookingAsync(Guid userId, BookingDto bookingDto, CancellationToken token = default);
    Task<bool> DeleteBookingAsync(Guid id, CancellationToken token = default);
    Task<BookingDto?> UpdateBookingAsync(Guid id, BookingDto bookingDto, CancellationToken token = default);
}