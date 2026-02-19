using Calendar_Service.Data;
using Calendar_Service.Services;
using MassTransit;
using MassTransit.Internals.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Calendar.UnitTests.Services;

public class PostgresCalendarServiceTests
{
    private readonly ICalendarService _service;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PostgresCalendarService> _logger;
    private readonly IPublishEndpoint _endPoint;
    private readonly CancellationTokenSource _token = new();

    public PostgresCalendarServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _logger = Substitute.For<ILogger<PostgresCalendarService>>();
        _endPoint = Substitute.For<IPublishEndpoint>();
        _service = new PostgresCalendarService(_context, _logger);
    }
    
    
}