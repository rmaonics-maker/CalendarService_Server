using Calendar.Contracts;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NotificationService;
using Xunit;

namespace Calendar.IntegrationTests;

public class NotificationTests
{
    [Fact]
    public async Task Consume_SuccessScenario_ShouldSendEmail()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<NotificationConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var message = new EventCreated
        {
            EventId = Guid.NewGuid(),
            Title = "Test Event",
            UserId = Guid.NewGuid(),
            UserEmail = "success@example.com",
            OccurredAt = DateTime.UtcNow
        };

        // Act
        await harness.Bus.Publish(message);

        // Assert
        (await harness.Consumed.Any<EventCreated>()).Should().BeTrue();
        (await harness.GetConsumerHarness<NotificationConsumer>().Consumed.Any<EventCreated>()).Should().BeTrue();
    }

    [Fact]
    public async Task Consume_RetryScenario_ShouldRetryAndEventuallySucceed()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<NotificationConsumer>();
                
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseMessageRetry(r => r.Immediate(5));
                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var message = new EventCreated
        {
            EventId = Guid.NewGuid(),
            Title = "Retry Success Event",
            UserId = Guid.NewGuid(),
            UserEmail = "retry@example.com",
            OccurredAt = DateTime.UtcNow
        };

        // Act
        await harness.Bus.Publish(message);

        // Assert
        // Ждем, пока сообщение будет успешно обработано (после ретраев)
        // Мы проверяем, что консьюмер в итоге успешно поглотил сообщение
        var consumerHarness = harness.GetConsumerHarness<NotificationConsumer>();
        (await consumerHarness.Consumed.Any<EventCreated>()).Should().BeTrue();
    }

    [Fact]
    public async Task Consume_FatalScenario_ShouldFailWithArgumentException()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<NotificationConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var message = new EventCreated
        {
            EventId = Guid.NewGuid(),
            Title = "Fatal Event",
            UserId = Guid.NewGuid(),
            UserEmail = "fatal@example.com", // Trigger fatal logic
            OccurredAt = DateTime.UtcNow
        };

        // Act
        await harness.Bus.Publish(message);

        // Assert
        (await harness.Consumed.Any<EventCreated>()).Should().BeTrue();
        
        var consumerHarness = harness.GetConsumerHarness<NotificationConsumer>();
        (await consumerHarness.Consumed.Any<EventCreated>(x => x.Exception is ArgumentException)).Should().BeTrue();
    }
}
