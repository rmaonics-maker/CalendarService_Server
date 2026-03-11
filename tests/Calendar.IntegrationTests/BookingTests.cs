using System.Net.Http.Json;
using Calendar.Contracts;
using Calendar.Contracts.Events;
using FluentAssertions;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;

namespace Calendar.IntegrationTests
{
    [Collection(nameof(IntegrationTestCollection))]
    public class BookingTests(CalendarApiFactory factory) : BaseIntegrationTest(factory)
    {
        [Fact]
        public async Task Post_Should_ReturnsCreatedAndResistInDb_When_PostBookingEvent()
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
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/events")
            {
                Content = JsonContent.Create(createEventRequest)
            };
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
            
            var response = await Client.SendAsync(request);

            // Assert - HTTP Response
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created,
                "API должен вернуть статус 201 Created при успешном создании события");

            // Assert - Database
            var bookingInDb = DbContext.Bookings.FirstOrDefault(b => b.UserId == user.Id);
            bookingInDb.Should().NotBeNull("Бронирование должно быть сохранено в базе данных");
            bookingInDb!.Title.Should().Be(createEventRequest.Title, "Заголовок должен соответствовать запросу");
            bookingInDb.UserId.Should().Be(user.Id, "UserId должен соответствовать созданному пользователю");
            bookingInDb.RoomId.Should().BeGreaterThan(0, "RoomId должен быть положительным числом");
            
            // Проверяем, что период бронирования был создан (не пустой)
            bookingInDb.Period.Should().NotBeNull("Период бронирования должен быть заполнен");
            
            // Проверяем, что баланс пользователя уменьшился на стоимость бронирования (10.0)
            // Используем AsNoTracking для получения свежих данных из базы без кэширования
            var updatedUser = DbContext.Users.AsNoTracking().FirstOrDefault(u => u.Id == user.Id);
            updatedUser.Should().NotBeNull("Пользователь должен существовать");
            updatedUser!.Balance.Should().Be(user.Balance - 10.0m, "Баланс должен уменьшиться на стоимость бронирования");

            // Assert - MassTransit Message Bus
            // Проверяем, что сообщение было отправлено в шину
            var messageSent = await Harness.Published.Any<EventCreated>(x => x.Context.Message.UserId == user.Id);
            messageSent.Should().BeTrue("Сообщение EventCreated должно быть опубликовано в шине сообщений");

            // Получаем опубликованное сообщение для детальной проверки
            var publishedMessages = new List<IPublishedMessage<EventCreated>>();
            await foreach (var message in Harness.Published.SelectAsync<EventCreated>(x => x.Context.Message.UserId == user.Id))
            {
                publishedMessages.Add(message);
            }
            
            publishedMessages.Should().HaveCount(1, "Должно быть опубликовано ровно одно сообщение EventCreated");
            var eventCreatedMessage = publishedMessages.First();
            
            eventCreatedMessage.Context.Message.Title.Should().Be(createEventRequest.Title, "Заголовок в сообщении должен соответствовать запросу");
            eventCreatedMessage.Context.Message.UserId.Should().Be(user.Id, "UserId в сообщении должен соответствовать созданному пользователю");
            eventCreatedMessage.Context.Message.EventId.Should().NotBeEmpty("EventId должен быть заполнен");
            eventCreatedMessage.Context.Message.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10),
                "OccuredAt должен быть близок к текущему времени");
        }
    }
}
