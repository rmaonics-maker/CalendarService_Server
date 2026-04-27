using AutoMapper;
using Calendar_Service.Data;
using Calendar_Service.Services;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Calendar.UnitTests.Services;

public class PostgresAppointmentServiceTests
{
    private readonly IAppointmentService _service;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PostgresAppointmentService> _logger;
    private readonly IPublishEndpoint _endPoint;
    private readonly IMapper _mapper;

    public PostgresAppointmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _logger = Substitute.For<ILogger<PostgresAppointmentService>>();
        _endPoint = Substitute.For<IPublishEndpoint>();
        _mapper = Substitute.For<IMapper>();
        _service = new PostgresAppointmentService(_context, _endPoint, _mapper, _logger);
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
