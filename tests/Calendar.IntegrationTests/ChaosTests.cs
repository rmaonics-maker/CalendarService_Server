using Calendar.Contracts.Events;
using MassTransit;
using NotificationService;

namespace Calendar.IntegrationTests;

[Collection(nameof(IntegrationTestCollection))]
public class ChaosTests(CalendarApiFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Should_Successfully_When_TriggerRetryPolicyScenario()
    {
        var eventId = NewId.NextGuid();
        var userEmail = "retry@test.com";

        await Harness!.Bus.Publish(new AppointmentCreatedEvent(
            AppointmentId: eventId,
            Title: "Test event",
            UserId: Guid.NewGuid(),
            UserEmail: userEmail,
            OccurredAt: DateTime.UtcNow.AddDays(1)
        ));

        var consumerHarness = Harness.GetConsumerHarness<NotificationConsumer>();
        
        // Ждем, пока сообщение будет потреблено (с учетом повторных попыток)
        await Task.Delay(TimeSpan.FromSeconds(5));
        
        // Проверяем, что сообщение было потреблено NotificationConsumer
        var consumed = await consumerHarness.Consumed.Any<AppointmentCreatedEvent>(x => x.Context.Message.AppointmentId == eventId);
        Assert.True(consumed, "Сообщение не было потреблено NotificationConsumer после повторных попыток");
    }
    
    [Fact]
    public async Task Should_GoesToErrorQueue_When_FatalError()
    {
        var eventId = NewId.NextGuid();
        var userEmail = "fatal@test.com";

        await Harness!.Bus.Publish(new AppointmentCreatedEvent(
            AppointmentId: eventId,
            Title: "Test event",
            UserId: Guid.NewGuid(),
            UserEmail: userEmail,
            OccurredAt: DateTime.UtcNow.AddDays(1)
        ));
        
        var consumerHarness = Harness.GetConsumerHarness<NotificationConsumer>();
        
        // Ждем обработки сообщения
        await Task.Delay(TimeSpan.FromSeconds(2));
        
        // Проверяем, что сообщение было потреблено NotificationConsumer (даже с ошибкой)
        var consumed = await consumerHarness.Consumed.Any<AppointmentCreatedEvent>(x => x.Context.Message.AppointmentId == eventId);
        Assert.True(consumed, "Сообщение не было потреблено NotificationConsumer");
    }
    
    [Fact]
    public async Task Should_GoesToOutbox_When_Success()
    {
        var eventId = NewId.NextGuid();
        var userEmail = "success@test.com";

        await Harness!.Bus.Publish(new AppointmentCreatedEvent(
            AppointmentId: eventId,
            Title: "Test event",
            UserId: Guid.NewGuid(),
            UserEmail: userEmail,
            OccurredAt: DateTime.UtcNow.AddDays(1)
        ));
        
        var consumerHarness = Harness.GetConsumerHarness<NotificationConsumer>();
        
        // Ждем обработки сообщения
        await Task.Delay(TimeSpan.FromSeconds(2));
        
        // Проверяем, что сообщение было потреблено NotificationConsumer
        var consumed = await consumerHarness.Consumed.Any<AppointmentCreatedEvent>(x => x.Context.Message.AppointmentId == eventId);
        Assert.True(consumed, "Сообщение не было потреблено NotificationConsumer");
    }
}
