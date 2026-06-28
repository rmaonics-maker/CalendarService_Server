using AutoMapper;
using Calendar_Service.Data;
using Calendar_Service.Services;
using FluentAssertions;
using MassTransit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NodaTime;
using NSubstitute;

namespace Calendar.UnitTests.Services;

public class PostgresAppointmentServiceTests : IDisposable
{
    private readonly IAppointmentService _service;
    private readonly ApplicationDbContext _context;
    private readonly SqliteConnection _connection;

    public PostgresAppointmentServiceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TestDbContext(options);
        _context.Database.EnsureCreated();

        var logger = Substitute.For<ILogger<PostgresAppointmentService>>();
        var endPoint = Substitute.For<IPublishEndpoint>();
        var mapper = Substitute.For<IMapper>();
        var timeProvider = new FakeTimeProvider();
        var recurrenceEvaluator = Substitute.For<IRecurrenceEvaluator>();
        var bookingPublisher = Substitute.For<IBookingPublisher>();
        _service = new PostgresAppointmentService(_context, endPoint, mapper, logger, timeProvider, recurrenceEvaluator, bookingPublisher);
    }

    private class TestDbContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
    {
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<Instant>().HaveConversion<InstantValueConverter>();
            configurationBuilder.Properties<Instant?>().HaveConversion<NullableInstantValueConverter>();
        }
    }

    private class InstantValueConverter() : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<Instant, string>(
        v => v.ToString(),
        v => NodaTime.Text.InstantPattern.ExtendedIso.Parse(v).Value);

    private class NullableInstantValueConverter() : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<Instant?, string?>(
        v => v.HasValue ? v.Value.ToString() : null,
        v => v != null ? NodaTime.Text.InstantPattern.ExtendedIso.Parse(v).Value : null);

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
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
