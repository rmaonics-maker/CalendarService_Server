using Calendar_Service.Contracts;
using System.Net.Http.Json;

namespace Calendar.IntegrationTests
{
    public class BookingTests(CalendarApiFactory factory) : BaseIntegrationTest(factory)
    {
        [Fact]
        public async Task Post_Booking_ReturnsCreatedAndResistInDb()
        {
            // Arrange
            var user = await CreateUserInDbAsync();
            var createEventRequest = new CreateEventRequest(
                Title: "Team Meeting",
                Description: "Weekly sync-up with the team",
                StartTime: DateTimeOffset.UtcNow.AddDays(1),
                Duration: TimeSpan.FromHours(1),
                Location: "Conference Room A",
                RecurrentRule: null,
                UserId: user.Id);

            // Act
            var response = await Client.PostAsJsonAsync("/api/v1/events", createEventRequest);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

            var bookingInDb = DbContext.Bookings.FirstOrDefault(b => b.UserId == user.Id);

            Assert.NotNull(bookingInDb);
        }
    }
}
