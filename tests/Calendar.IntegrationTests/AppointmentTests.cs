using System.Net.Http.Json;
using Calendar.Contracts.Events;
using Calendar.Contracts.Requests;
using FluentAssertions;
using MassTransit.Testing;

namespace Calendar.IntegrationTests
{
    [Collection(nameof(IntegrationTestCollection))]
    public class AppointmentTests(CalendarApiFactory factory) : BaseIntegrationTest(factory)
    {
        [Fact]
        public async Task Post_Should_ReturnsCreatedAndResistInDb_When_PostAppointment()
        {
            // Arrange
            var user = await CreateUserInDbAsync();
            var createAppointmentRequest = new CreateAppointmentRequest(
                Title: "Team Meeting",
                Description: "Weekly sync-up with the team",
                StartTime: DateTimeOffset.UtcNow.AddDays(1),
                Duration: TimeSpan.FromHours(1),
                Location: "Conference Room A",
                RecurrentRule: null);

            // Act
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/events")
            {
                Content = JsonContent.Create(createAppointmentRequest)
            };
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
            
            // Настраиваем аутентификацию для конкретного пользователя, если это возможно в TestAuthHandler
            // В данном случае TestAuthHandler использует жестко заданный ID. 
            // Для интеграционного теста нам важно, чтобы ID совпадал.
            // Я обновлю TestAuthHandler позже или подстроюсь под него.
            
            var response = await Client.SendAsync(request);

            // Assert - HTTP Response
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created,
                "API должен вернуть статус 201 Created при успешном создании события");

            // Assert - Database
            var appointmentInDb = DbContext.Appointments.FirstOrDefault(b => b.Title == createAppointmentRequest.Title);
            appointmentInDb.Should().NotBeNull("Встреча должна быть сохранена в базе данных");
            appointmentInDb!.Title.Should().Be(createAppointmentRequest.Title, "Заголовок должен соответствовать запросу");
            
            // Проверяем, что период был создан (не пустой)
            appointmentInDb.Slot.Should().NotBeNull("Период должен быть заполнен");
            
            // Assert - MassTransit Message Bus
            // Проверяем, что сообщение было опубликовано в шине
            var messageSent = await Harness!.Published.Any<AppointmentCreatedEvent>(x => x.Context.Message.Title == createAppointmentRequest.Title);
            messageSent.Should().BeTrue("Сообщение AppointmentCreatedEvent должно быть опубликовано в шине сообщений");

            // Получаем опубликованное сообщение для детальной проверки
            var publishedMessages = new List<IPublishedMessage<AppointmentCreatedEvent>>();
            await foreach (var message in Harness.Published.SelectAsync<AppointmentCreatedEvent>(x => x.Context.Message.Title == createAppointmentRequest.Title))
            {
                publishedMessages.Add(message);
            }
            
            publishedMessages.Should().HaveCount(1, "Должно быть опубликовано ровно одно сообщение AppointmentCreatedEvent");
            var eventCreatedMessage = publishedMessages.First();
            
            eventCreatedMessage.Context.Message.Title.Should().Be(createAppointmentRequest.Title, "Заголовок в сообщении должен соответствовать запросу");
            eventCreatedMessage.Context.Message.AppointmentId.Should().NotBeEmpty("AppointmentId должен быть заполнен");
        }
    }
}
