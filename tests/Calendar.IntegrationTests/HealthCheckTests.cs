using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Calendar.IntegrationTests
{
    [Collection(nameof(IntegrationTestCollection))]
    public class HealthCheckTests : BaseIntegrationTest
    {
        public HealthCheckTests(CalendarApiFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Health_ShouldReturnOk()
        {
            // Arrange

            // Act
            var response = await Client.GetAsync("/health");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }
    }
}
