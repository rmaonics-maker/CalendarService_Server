using System.Security.Claims;
using AutoMapper;
using Calendar.Booking.API.Controllers;
using Calendar.Booking.API.Services;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Requests;
using Calendar.Contracts.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using FluentAssertions;
using System.IdentityModel.Tokens.Jwt;

namespace Booking.UnitTests.Controllers;

[TestFixture]
public class BookingsControllerTests
{
    private IBookingService _service = null!;
    private IMapper _mapper = null!;
    private BookingsController _controller = null!;
    private Guid _currentUserId;

    [SetUp]
    public void SetUp()
    {
        _service = Substitute.For<IBookingService>();
        _mapper = Substitute.For<IMapper>();
        _controller = new BookingsController(_service, _mapper);
        _currentUserId = Guid.NewGuid();

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, _currentUserId.ToString())
        }, "TestAuthentication"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Test]
    public async Task GetBookings_Should_ReturnOk_When_UserIsAuthorized()
    {
        // Arrange
        var bookings = new List<BookingDto> { new BookingDto(Guid.NewGuid(), "Title", 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow, null) };
        var responses = bookings.Select(b => new BookingResponse(b.Id, b.Title, _currentUserId, b.RoomId, b.StartTimeUtc, b.EndTimeUtc, b.CreatedAtUtc, b.AppointmentId)).ToList();
        
        _service.GetBookingsAsync(_currentUserId, Arg.Any<CancellationToken>()).Returns(bookings);
        _mapper.Map<IEnumerable<BookingResponse>>(bookings).Returns(responses);

        // Act
        var result = await _controller.GetBookings();

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(responses);
    }

    [Test]
    public async Task GetBooking_Should_ReturnOk_When_BookingExistsAndBelongsToUser()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var bookingDto = new BookingDto(bookingId, "Title", 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow, null);
        var response = new BookingResponse(bookingId, "Title", _currentUserId, 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow, null);
        
        _service.GetBookingAsync(bookingId, Arg.Any<CancellationToken>()).Returns((_currentUserId, bookingDto));
        _mapper.Map<BookingResponse>(bookingDto).Returns(response);

        // Act
        var result = await _controller.GetBooking(bookingId);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(response);
    }


    [Test]
    public async Task GetBooking_Should_ReturnNotFound_When_BookingDoesNotExist()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        _service.GetBookingAsync(bookingId, Arg.Any<CancellationToken>()).Returns((null, null));

        // Act
        var result = await _controller.GetBooking(bookingId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task GetBooking_Should_ReturnForbid_When_BookingDoesNotBelongToUser()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var bookingDto = new BookingDto(bookingId, "Title", 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow, null);
        _service.GetBookingAsync(bookingId, Arg.Any<CancellationToken>()).Returns((otherUserId, bookingDto));

        // Act
        var result = await _controller.GetBooking(bookingId);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Test]
    public async Task CreateBooking_Should_ReturnCreatedAtAction_When_RequestIsValid()
    {
        // Arrange
        var request = new CreateBookingRequest("Meeting", 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), null);
        var bookingDto = new BookingDto(Guid.Empty, "Meeting", 1, request.StartTimeUtc, request.EndTimeUtc, DateTimeOffset.UtcNow, null);
        var createdBooking = bookingDto with { Id = Guid.NewGuid() };
        var response = new BookingResponse(createdBooking.Id, createdBooking.Title, _currentUserId, createdBooking.RoomId, createdBooking.StartTimeUtc, createdBooking.EndTimeUtc, createdBooking.CreatedAtUtc, null);

        _mapper.Map<BookingDto>(request).Returns(bookingDto);
        _service.CreateBookingAsync(_currentUserId, Arg.Any<BookingDto>(), Arg.Any<CancellationToken>()).Returns(createdBooking);
        _mapper.Map<BookingResponse>(createdBooking).Returns(response);

        // Act
        var result = await _controller.CreateBooking(request);

        // Assert
        var createdAtActionResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdAtActionResult.ActionName.Should().Be(nameof(BookingsController.GetBooking));
        createdAtActionResult.RouteValues!["id"].Should().Be(createdBooking.Id);
        createdAtActionResult.Value.Should().Be(response);
    }

    [Test]
    public async Task DeleteBooking_Should_ReturnNoContent_When_DeleteIsSuccessful()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var bookingDto = new BookingDto(bookingId, "Title", 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow, null);
        _service.GetBookingAsync(bookingId, Arg.Any<CancellationToken>()).Returns((_currentUserId, bookingDto));
        _service.DeleteBookingAsync(bookingId, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _controller.DeleteBooking(bookingId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }
}
