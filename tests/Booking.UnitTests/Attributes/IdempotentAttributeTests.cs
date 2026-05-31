using System.Text.Json;
using Calendar.Booking.API.Attributes;
using Calendar.Booking.API.Data;
using Calendar.Booking.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using FluentAssertions;

namespace Booking.UnitTests.Attributes;

[TestFixture]
public class IdempotentAttributeTests
{
    private BookingDbContext _dbContext = null!;
    private IdempotentAttribute _attribute = null!;
    private IServiceProvider _serviceProvider = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        // Use a simplified DbContext to avoid Postgres-specific extensions in OnModelCreating
        _dbContext = new TestBookingDbContext(options);
        _dbContext.Database.EnsureCreated();

        var services = new ServiceCollection();
        services.AddSingleton(_dbContext);
        _serviceProvider = services.BuildServiceProvider();

        _attribute = new IdempotentAttribute();
    }

    [TearDown]
    public void TearDown()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        _dbContext.Dispose();
    }

    private ActionExecutingContext CreateContext(HeaderDictionary headers)
    {
        var httpContext = new DefaultHttpContext();
        foreach (var header in headers)
        {
            httpContext.Request.Headers[header.Key] = header.Value;
        }
        httpContext.RequestServices = _serviceProvider;

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor()
        );

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            Substitute.For<Controller>()
        );
    }

    [Test]
    public async Task OnActionExecutionAsync_Should_ReturnBadRequest_When_IdempotencyKeyHeaderIsMissing()
    {
        // Arrange
        var context = CreateContext(new HeaderDictionary());
        ActionExecutionDelegate next = () => Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), Substitute.For<Controller>()));

        // Act
        await _attribute.OnActionExecutionAsync(context, next);

        // Assert
        context.Result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be("Idempotency-Key header is required");
    }

    [Test]
    public async Task OnActionExecutionAsync_Should_ReturnCachedResult_When_KeyExistsAndNotExpired()
    {
        // Arrange
        var key = "test-key";
        var expectedValue = new { Message = "Success" };
        var cachedRecord = new IdempotencyKeyModel
        {
            Key = key,
            StatusCode = 200,
            ResponseBody = JsonSerializer.Serialize(expectedValue),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        _dbContext.IdempotencyKeys.Add(cachedRecord);
        await _dbContext.SaveChangesAsync();

        var context = CreateContext(new HeaderDictionary { { "Idempotency-Key", key } });
        ActionExecutionDelegate next = Substitute.For<ActionExecutionDelegate>();

        // Act
        await _attribute.OnActionExecutionAsync(context, next);

        // Assert
        context.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(200);
        
        await next.DidNotReceive().Invoke();
    }

    [Test]
    public async Task OnActionExecutionAsync_Should_ExecuteNextAndCache_When_KeyDoesNotExist()
    {
        // Arrange
        var key = "new-key";
        var resultValue = new { Id = 1 };
        var context = CreateContext(new HeaderDictionary { { "Idempotency-Key", key } });
        
        var actionExecutedContext = new ActionExecutedContext(context, new List<IFilterMetadata>(), Substitute.For<Controller>())
        {
            Result = new ObjectResult(resultValue) { StatusCode = 201 }
        };
        
        ActionExecutionDelegate next = () => Task.FromResult(actionExecutedContext);

        // Act
        await _attribute.OnActionExecutionAsync(context, next);

        // Assert
        var record = await _dbContext.IdempotencyKeys.FindAsync(key);
        record.Should().NotBeNull();
        record!.StatusCode.Should().Be(201);
        record.ResponseBody.Should().Be(JsonSerializer.Serialize(resultValue));
    }

    // Simplified DbContext for testing
    private class TestBookingDbContext(DbContextOptions<BookingDbContext> options) : BookingDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IdempotencyKeyModel>().HasKey(x => x.Key);
        }
    }
}
