using AutoMapper;
using Calendar.Booking.API.Mapping;
using Calendar.Booking.API.Models;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Requests;
using FluentAssertions;
using NpgsqlTypes;
using Microsoft.Extensions.DependencyInjection;

namespace Booking.UnitTests.Mapping;

[TestFixture]
public class BookingMappingProfileTests
{
    private IMapper _mapper = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddProfile<BookingMappingProfile>());
        var serviceProvider = services.BuildServiceProvider();
        _mapper = serviceProvider.GetRequiredService<IMapper>();
    }

    [Test]
    public void BookingMappingProfile_Should_HaveValidConfiguration()
    {
        _mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }

    [Test]
    public void Map_Should_MapCreateBookingRequestToBookingDto()
    {
        // Arrange
        var request = new CreateBookingRequest("Meeting", 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), null);

        // Act
        var result = _mapper.Map<BookingDto>(request);

        // Assert
        result.Title.Should().Be(request.Title);
        result.RoomId.Should().Be(request.RoomId);
        result.StartTimeUtc.Should().Be(request.StartTimeUtc);
        result.EndTimeUtc.Should().Be(request.EndTimeUtc);
        result.Id.Should().NotBeEmpty();
    }

    [Test]
    public async Task Map_Should_MapBookingDtoToBookingModel()
    {
        // Arrange
        var dto = new BookingDto(Guid.NewGuid(), "Title", 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow, null);

        // Act
        var result = _mapper.Map<BookingModel>(dto);

        // Assert
        result.Title.Should().Be(dto.Title);
        result.RoomId.Should().Be(dto.RoomId);
        result.Period.LowerBound.Should().Be(dto.StartTimeUtc.UtcDateTime);
        result.Period.UpperBound.Should().Be(dto.EndTimeUtc.UtcDateTime);
    }

    [Test]
    public void Map_Should_MapBookingModelToBookingDto()
    {
        // Arrange
        var period = new NpgsqlRange<DateTime>(DateTime.UtcNow, DateTime.UtcNow.AddHours(1));
        var model = new BookingModel
        {
            Id = Guid.NewGuid(),
            Title = "Title",
            RoomId = 1,
            UserId = Guid.NewGuid(),
            Period = period,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var result = _mapper.Map<BookingDto>(model);

        // Assert
        result.Id.Should().Be(model.Id);
        result.Title.Should().Be(model.Title);
        result.RoomId.Should().Be(model.RoomId);
        result.StartTimeUtc.Should().Be(model.Period.LowerBound);
        result.EndTimeUtc.Should().Be(model.Period.UpperBound);
    }
}
