using System.Net.Http.Json;
using Calendar.Contracts;
using MassTransit;
using Xunit;

namespace Calendar.IntegrationTests
{
    [Collection(nameof(IntegrationTestCollection))]
    public class BookingTests(CalendarApiFactory factory) : BaseIntegrationTest(factory)
    {
        //[Fact]
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
            // 1. Проверяем, что API вернул успешный статус
            Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

            // 2. Проверяем, что бронирование сохранилось в базе данных
            var bookingInDb = DbContext.Bookings.FirstOrDefault(b => b.UserId == user.Id);
            Assert.NotNull(bookingInDb);

            // 3. Проверяем, что сообщение было опубликовано и потреблено
            //var consumed = await Harness.Consumed.Any<EventCreated>(x => x.Context.Message.UserId == user.Id);
            //Assert.True(consumed, "Сообщение EventCreated не было потреблено");
        }
    }
}
