using Calendar.Contracts;
using MassTransit;
//using NotificationService;

namespace Calendar.IntegrationTests;

[Collection(nameof(IntegrationTestCollection))]
public class ChaosTests(CalendarApiFactory factory) : BaseIntegrationTest(factory)
{
    //[Fact]
    public async Task Trigger_RetryPolicyScenario_Successfully()
    {
        var eventId = NewId.NextGuid();
        var userEmail = "retry@test.com";
/*
        await Harness.Bus.Publish(new EventCreated 
        { 
            EventId = eventId,
            Title = "Test event",
            UserEmail = userEmail,
            UserId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow.AddDays(1)
        });

    //var consumerHarness = Harness.GetConsumerHarness<NotificationConsumer>();
        
        //Assert.True(await consumerHarness.Consumed.Any<EventCreated>(x => x.Context.Message.EventId == eventId));
        
        var sent = await Harness.Sent.Any<EventCreated>(x => x.Context.Message.EventId == eventId);
        Assert.True(sent);*/
    }
    
    //[Fact]
    public async Task FatalError_GoesToErrorQueue()
    {
        var eventId = NewId.NextGuid();
        var userEmail = "fatal@test.com";
/*
        await Harness.Bus.Publish(new EventCreated 
        { 
            EventId = eventId,
            Title = "Test event",
            UserEmail = userEmail,
            UserId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow.AddDays(1)
        });
        
        //var consumerHarness = Harness.GetConsumerHarness<NotificationConsumer>();
        
        //Assert.True(await consumerHarness.Consumed.Any<EventCreated>(x => x.Context.Message.EventId == eventId));
        
        var sent = await Harness.Sent.Any<Fault<EventCreated>>(x => x.Context.Message.Message.EventId == eventId);
        Assert.True(sent);*/
    }
    
    //[Fact]
    public async Task Success_GoesToOutbox()
    {
        var eventId = NewId.NextGuid();
        var userEmail = "success@test.com";
/*
        await Harness.Bus.Publish(new EventCreated 
        { 
            EventId = eventId,
            Title = "Test event",
            UserEmail = userEmail,
            UserId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow.AddDays(1)
        });
        
        //var consumerHarness = Harness.GetConsumerHarness<NotificationConsumer>();
        
        //Assert.True(await consumerHarness.Consumed.Any<EventCreated>(x => x.Context.Message.EventId == eventId));
        
        var sent = await Harness.Sent.Any<EventCreated>(x => x.Context.Message.EventId == eventId);
        Assert.True(sent);*/
    }
}
