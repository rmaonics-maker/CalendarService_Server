using System.Text.Json;
using Calendar.Booking.API.Options;
using Calendar.Booking.API.Services;
using Calendar.Contracts.Dtos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using FluentAssertions;

namespace Booking.UnitTests.Services;

[TestFixture]
public class CachedBookingServiceTests
{
    private IBookingService _inner = null!;
    private IDistributedCache _cache = null!;
    private IOptions<CacheOptions> _options = null!;
    private ILogger<CachedBookingService> _logger = null!;
    private CachedBookingService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _inner = Substitute.For<IBookingService>();
        _cache = Substitute.For<IDistributedCache>();
        _options = Options.Create(new CacheOptions { BaseTltMinutes = 10, JitterMinutes = 2 });
        _logger = Substitute.For<ILogger<CachedBookingService>>();
        _service = new CachedBookingService(_inner, _cache, _options, _logger);
    }

    [Test]
    public async Task GetBookingsAsync_Should_ReturnFromInner_And_CacheResult_When_CacheIsEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookings = new List<BookingDto> { new BookingDto(Guid.NewGuid(), "Title", 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow, null) };
        var cacheKey = $"bookings:user:{userId}";

        _cache.GetAsync(cacheKey, Arg.Any<CancellationToken>()).Returns((byte[]?)null);
        _inner.GetBookingsAsync(userId, Arg.Any<CancellationToken>()).Returns(bookings);

        // Act
        var result = await _service.GetBookingsAsync(userId);

        // Assert
        result.Should().BeEquivalentTo(bookings);
        await _inner.Received(1).GetBookingsAsync(userId, Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(cacheKey, Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetBookingsAsync_Should_ReturnFromCache_When_CacheIsNotEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookings = new List<BookingDto> { new BookingDto(Guid.NewGuid(), "Title", 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow, null) };
        var cacheKey = $"bookings:user:{userId}";
        var serialized = JsonSerializer.SerializeToUtf8Bytes(bookings);

        _cache.GetAsync(cacheKey, Arg.Any<CancellationToken>()).Returns(serialized);

        // Act
        var result = await _service.GetBookingsAsync(userId);

        // Assert
        result.Should().BeEquivalentTo(bookings);
        await _inner.DidNotReceive().GetBookingsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateBookingAsync_Should_InvalidateUserCache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookingDto = new BookingDto(Guid.Empty, "Meeting", 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow, null);
        var createdBooking = bookingDto with { Id = Guid.NewGuid() };
        _inner.CreateBookingAsync(userId, bookingDto, Arg.Any<CancellationToken>()).Returns(createdBooking);

        // Act
        await _service.CreateBookingAsync(userId, bookingDto);

        // Assert
        await _cache.Received(1).RemoveAsync($"bookings:user:{userId}", Arg.Any<CancellationToken>());
    }
}
