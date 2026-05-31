using AutoMapper;
using Calendar.Booking.API.Data;
using Calendar.Booking.API.Models;
using Calendar.Booking.API.Services;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using FluentAssertions;
using NpgsqlTypes;
using Microsoft.Extensions.DependencyInjection;
using MassTransit;

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Booking.UnitTests.Services;

[TestFixture]
public class PostgresBookingServiceTests
{
    private BookingDbContext _context = null!;
    private IPublishEndpoint _publishEndpoint = null!;
    private ILogger<PostgresBookingService> _logger = null!;
    private IMapper _mapper = null!;
    private FakeTimeProvider _timeProvider = null!;
    private IDistributedLockService _lockService = null!;
    private PostgresBookingService _service = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        
        _context = new TestBookingDbContext(options);
        
        _publishEndpoint = Substitute.For<IPublishEndpoint>();
        _logger = Substitute.For<ILogger<PostgresBookingService>>();
        
        // Real mapper for ProjectTo and mapping logic
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => {
            cfg.AddProfile<Calendar.Booking.API.Mapping.BookingMappingProfile>();
        });
        var sp = services.BuildServiceProvider();
        _mapper = sp.GetRequiredService<IMapper>();

        _timeProvider = new FakeTimeProvider();
        _lockService = Substitute.For<IDistributedLockService>();
        
        _service = new PostgresBookingService(
            _context, 
            _publishEndpoint, 
            _logger, 
            _mapper, 
            _timeProvider, 
            _lockService);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task GetBookingAsync_Should_ReturnBooking_When_Exists()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var bookingModel = new BookingModel 
        { 
            Id = bookingId, 
            UserId = userId, 
            Title = "Meeting",
            Period = new NpgsqlRange<DateTime>(DateTime.UtcNow, DateTime.UtcNow.AddHours(1)),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _context.Bookings.Add(bookingModel);
        await _context.SaveChangesAsync();

        // Act
        var (returnedUserId, result) = await _service.GetBookingAsync(bookingId);

        // Assert
        returnedUserId.Should().Be(userId);
        result.Should().NotBeNull();
        result!.Id.Should().Be(bookingId);
        result.Title.Should().Be("Meeting");
    }

    [Test]
    public async Task GetBookingAsync_Should_ReturnNull_When_DoesNotExist()
    {
        // Act
        var (returnedUserId, result) = await _service.GetBookingAsync(Guid.NewGuid());

        // Assert
        returnedUserId.Should().BeNull();
        result.Should().BeNull();
    }

    [Test]
    public async Task GetBookingsAsync_Should_ReturnUserBookings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        
        _context.Bookings.AddRange(
            new BookingModel { Id = Guid.NewGuid(), UserId = userId, Title = "B1", Period = new NpgsqlRange<DateTime>(DateTime.UtcNow, DateTime.UtcNow.AddHours(1)), CreatedAt = DateTimeOffset.UtcNow },
            new BookingModel { Id = Guid.NewGuid(), UserId = userId, Title = "B2", Period = new NpgsqlRange<DateTime>(DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3)), CreatedAt = DateTimeOffset.UtcNow },
            new BookingModel { Id = Guid.NewGuid(), UserId = otherUserId, Title = "B3", Period = new NpgsqlRange<DateTime>(DateTime.UtcNow, DateTime.UtcNow.AddHours(1)), CreatedAt = DateTimeOffset.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = (await _service.GetBookingsAsync(userId)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(b => b.Title.Should().StartWith("B"));
    }

    [Test]
    public async Task DeleteBookingAsync_Should_RemoveBooking_And_PublishEvent_When_Exists()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var bookingModel = new BookingModel 
        { 
            Id = bookingId, 
            UserId = Guid.NewGuid(), 
            Title = "To Delete", 
            Period = new NpgsqlRange<DateTime>(DateTime.UtcNow, DateTime.UtcNow.AddHours(1)),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _context.Bookings.Add(bookingModel);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteBookingAsync(bookingId);

        // Assert
        result.Should().BeTrue();
        _context.Bookings.Should().NotContain(b => b.Id == bookingId);
        await _publishEndpoint.Received(1).Publish(Arg.Any<BookingDeletedEvent>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteBookingAsync_Should_ReturnFalse_When_DoesNotExist()
    {
        // Act
        var result = await _service.DeleteBookingAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<BookingDeletedEvent>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateBookingAsync_Should_AddBooking_And_PublishEvent_When_Successful()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var startTime = DateTimeOffset.UtcNow.AddHours(1);
        var endTime = startTime.AddHours(2);
        var bookingDto = new BookingDto(Guid.Empty, "New Meeting", 1, startTime, endTime, DateTimeOffset.UtcNow, null);
        
        var mockLock = Substitute.For<IDistributedLock>();
        _lockService.AcquireAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(mockLock);

        // Act
        var result = await _service.CreateBookingAsync(userId, bookingDto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("New Meeting");
        result.RoomId.Should().Be(1);
        
        var dbBooking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == result.Id);
        dbBooking.Should().NotBeNull();
        dbBooking!.UserId.Should().Be(userId);
        
        await _publishEndpoint.Received(1).Publish(Arg.Any<BookingCreatedEvent>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateBookingAsync_Should_Throw_When_LockCannotBeAcquired()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookingDto = new BookingDto(Guid.Empty, "Meeting", 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow, null);
        
        _lockService.AcquireAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IDistributedLock?)null);

        // Act
        Func<Task> act = () => _service.CreateBookingAsync(userId, bookingDto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Не удалось получить блокировку*");
    }

    [Test]
    public async Task UpdateBookingAsync_Should_UpdateFields_And_PublishEvent()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existingBooking = new BookingModel 
        { 
            Id = bookingId, 
            UserId = userId, 
            Title = "Old Title", 
            RoomId = 1,
            Period = new NpgsqlRange<DateTime>(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _context.Bookings.Add(existingBooking);
        await _context.SaveChangesAsync();

        var updateDto = new BookingDto(bookingId, "New Title", 2, DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(1).AddHours(1), DateTimeOffset.UtcNow, null);
        
        var mockLock = Substitute.For<IDistributedLock>();
        _lockService.AcquireAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(mockLock);

        // Act
        var result = await _service.UpdateBookingAsync(bookingId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("New Title");
        result.RoomId.Should().Be(2);

        var dbBooking = await _context.Bookings.FindAsync(bookingId);
        dbBooking!.Title.Should().Be("New Title");
        dbBooking.RoomId.Should().Be(2);

        await _publishEndpoint.Received(1).Publish(Arg.Any<BookingUpdatedEvent>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateBookingAsync_Should_ReturnNull_When_BookingDoesNotExist()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var updateDto = new BookingDto(bookingId, "Title", 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow, null);
        
        var mockLock = Substitute.For<IDistributedLock>();
        _lockService.AcquireAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(mockLock);

        // Act
        var result = await _service.UpdateBookingAsync(bookingId, updateDto);

        // Assert
        result.Should().BeNull();
    }

    private class TestBookingDbContext(DbContextOptions<BookingDbContext> options) : BookingDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BookingModel>().HasKey(x => x.Id);
            modelBuilder.Entity<IdempotencyKeyModel>().HasKey(x => x.Key);
        }
    }
}
