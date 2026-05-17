using AutoMapper;
using Calendar_Service.Data;
using Calendar_Service.Services;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Calendar.UnitTests.Services;

public class PostgresAppointmentServiceTests
{
    private readonly IAppointmentService _service;

    public PostgresAppointmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        var logger = Substitute.For<ILogger<PostgresAppointmentService>>();
        var endPoint = Substitute.For<IPublishEndpoint>();
        var mapper = Substitute.For<IMapper>();
        var timeProvider = new FakeTimeProvider();
        _service = new PostgresAppointmentService(context, endPoint, mapper, logger, timeProvider);
    }
    
    [Fact]
    public async Task GetAppointmentAsync_Should_ReturnNull_When_NotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = await _service.GetAppointmentAsync(id);

        // Assert
        result.Should().BeNull();
    }
}
