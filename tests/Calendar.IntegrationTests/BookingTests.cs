using System.Net;
using System.Net.Http.Json;
using Calendar.Contracts.Requests;
using Calendar.Contracts.Responses;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Calendar.IntegrationTests;

public class BookingTests(BookingApiFactory factory) : BookingBaseIntegrationTest(factory)
{
    [Fact]
    public async Task CreateBooking_Should_ReturnCreated_When_RequestIsValid()
    {
        // Arrange
        var request = new CreateBookingRequest(
            Title: "Team Meeting",
            RoomId: 1, // Use a seeded room ID
            StartTimeUtc: DateTimeOffset.UtcNow.AddHours(1),
            EndTimeUtc: DateTimeOffset.UtcNow.AddHours(2),
            AppointmentId: null
        );

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/bookings")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        // Act
        var response = await Client.SendAsync(httpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var booking = await response.Content.ReadFromJsonAsync<BookingResponse>();
        booking.Should().NotBeNull();
        booking!.Title.Should().Be(request.Title);
        booking.RoomId.Should().Be(request.RoomId);

        var dbBooking = await DbContext.Bookings.FirstOrDefaultAsync(b => b.Id == booking.Id);
        dbBooking.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBookings_Should_ReturnUserBookings()
    {
        // Arrange
        var request = new CreateBookingRequest(
            Title: "Meeting 1",
            RoomId: 1,
            StartTimeUtc: DateTimeOffset.UtcNow.AddHours(1),
            EndTimeUtc: DateTimeOffset.UtcNow.AddHours(2),
            AppointmentId: null
        );
        
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/bookings")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        await Client.SendAsync(httpRequest);

        // Act
        var response = await Client.GetAsync("/api/bookings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var bookings = await response.Content.ReadFromJsonAsync<IEnumerable<BookingResponse>>();
        bookings.Should().NotBeNull();
        bookings.Should().ContainSingle(b => b.Title == "Meeting 1");
    }

    [Fact]
    public async Task DeleteBooking_Should_ReturnNoContent_When_Exists()
    {
        // Arrange
        var request = new CreateBookingRequest(
            Title: "Meeting to Delete",
            RoomId: 1,
            StartTimeUtc: DateTimeOffset.UtcNow.AddHours(3),
            EndTimeUtc: DateTimeOffset.UtcNow.AddHours(4),
            AppointmentId: null
        );
        
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/bookings")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var createResponse = await Client.SendAsync(httpRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdBooking = await createResponse.Content.ReadFromJsonAsync<BookingResponse>();

        // Act
        var response = await Client.DeleteAsync($"/api/bookings/{createdBooking!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var dbBooking = await DbContext.Bookings.FindAsync(createdBooking.Id);
        dbBooking.Should().BeNull();
    }

    [Fact]
    public async Task CreateBooking_Should_ReturnCachedResponse_When_SameIdempotencyKeyIsUsed()
    {
        // Arrange
        var request = new CreateBookingRequest(
            Title: "Idempotent Meeting",
            RoomId: 1,
            StartTimeUtc: DateTimeOffset.UtcNow.AddHours(5),
            EndTimeUtc: DateTimeOffset.UtcNow.AddHours(6),
            AppointmentId: null
        );

        var idempotencyKey = Guid.NewGuid().ToString();

        var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/bookings")
        {
            Content = JsonContent.Create(request)
        };
        request1.Headers.Add("Idempotency-Key", idempotencyKey);

        // Act
        var response1 = await Client.SendAsync(request1);
        var booking1 = await response1.Content.ReadFromJsonAsync<BookingResponse>();

        var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/bookings")
        {
            Content = JsonContent.Create(request)
        };
        request2.Headers.Add("Idempotency-Key", idempotencyKey);
        var response2 = await Client.SendAsync(request2);
        var booking2 = await response2.Content.ReadFromJsonAsync<BookingResponse>();

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);
        booking1!.Id.Should().Be(booking2!.Id);

        var dbBookingsCount = await DbContext.Bookings.CountAsync(b => b.Title == "Idempotent Meeting");
        dbBookingsCount.Should().Be(1);
    }
}

