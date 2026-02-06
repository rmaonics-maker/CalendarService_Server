namespace Calendar.IntegrationTests;

public class ChaosTests(CalendarApiFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Trigger_RetryPolicyScenario_NothingAssert()
    {
        await Task.CompletedTask;
    }
}