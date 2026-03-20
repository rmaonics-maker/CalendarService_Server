using Calendar.Contracts.Events;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;

namespace NotificationService.Tests;

public class NotificationConsumerTests
{
    [Fact]
    public async Task Notification_Should_Be_Sent_When_Event_Created()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<NotificationConsumer>();
            })
            .AddSingleton<ILogger<NotificationConsumer>, NullLogger<NotificationConsumer>>()
            .BuildServiceProvider(true);
        
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var message = new EventCreated
        {
            AppointmentId = Guid.NewGuid(),
            Title = "Unit Test Event",
            UserId = Guid.NewGuid(),
            UserEmail = "test@unit.com",
            OccurredAt = DateTime.UtcNow
        };
        
        try 
        {
            // Act
            await harness.Bus.Publish(message);

            // Assert
            //сообщение попало в шину
            (await harness.Consumed.Any<EventCreated>()).Should().BeTrue();
            
            //потребитель обработал сообщение
            var consumerHarness = harness.GetConsumerHarness<NotificationConsumer>();
            (await consumerHarness.Consumed.Any<EventCreated>()).Should().BeTrue();
            
            // не было ошибок
            (await harness.Published.Any<Fault<EventCreated>>()).Should().BeFalse();
        }
        finally
        {
            await harness.Stop();
        }
    }

}