using AutoFixture;
using AutoMapper;
using Calendar_Service.Controllers;
using Calendar_Service.Models;
using Calendar_Service.Services;
using Calendar.Contracts;
using FluentAssertions;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit.Abstractions;

namespace Calendar.UnitTests.Controllers;

public class EventsControllerTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly EventsController _sut;
    private readonly ICalendarService _service;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateEventRequest> _validator;
    private readonly IPublishEndpoint _endPoint;
    private readonly IFixture _fixture;

    public EventsControllerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _service = Substitute.For<ICalendarService>();
        _mapper = Substitute.For<IMapper>();
        _validator = Substitute.For<IValidator<CreateEventRequest>>();
        _endPoint = Substitute.For<IPublishEndpoint>();
        _fixture = new Fixture();

        _validator.ValidateAsync(Arg.Any<CreateEventRequest>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        
        _sut = new EventsController(_service, _mapper, _validator, _endPoint);
    }
    
#region Create
    [Fact]
    public async Task Create_Should_ReturnsCreatedAtAction_When_ValidRequest()
    {
        // Arrange
        var request = _fixture.Create<CreateEventRequest>();
        var createdEventId = Guid.NewGuid();
        var userEmail = _fixture.Create<string>();
        var entity = _fixture.Create<CalendarEvent>();
        _mapper.Map<CalendarEvent>(request).Returns(entity);

        _testOutputHelper.WriteLine($"[DIAGNOSTIC] Test entity: {entity.Id}, Title: {entity.Title}");
        
        _service.CreateEventAsync(Arg.Any<CalendarEvent>(), _endPoint, Arg.Any<CancellationToken>())
            .Returns((createdEventId, userEmail));

        var eventResponse = new EventResponse(createdEventId, entity.Title, entity.Location, entity.StartTime,
            entity.StartTime.Add(entity.Duration));
        
        _mapper.Map<EventResponse>(Arg.Is<CalendarEvent>(e => e == entity)).Returns(eventResponse);
        _testOutputHelper.WriteLine($"[DIAGNOSTIC] Mock setup: Map<EventResponse>(entity with Id={entity.Id}) returns EventResponse");
        
        // Act
        var result = await _sut.Create(request, CancellationToken.None);

        // Assert
        _testOutputHelper.WriteLine($"[DIAGNOSTIC] Result type: {result.GetType().Name}");
        
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdAtActionResult = (CreatedAtActionResult)result;
        _testOutputHelper.WriteLine($"[DIAGNOSTIC] CreatedAtActionResult.ActionName: {createdAtActionResult.ActionName}");
        _testOutputHelper.WriteLine($"[DIAGNOSTIC] CreatedAtActionResult.Value: {createdAtActionResult.Value?.GetType().Name ?? "null"}");
        _testOutputHelper.WriteLine($"[DIAGNOSTIC] CreatedAtActionResult.RouteValues: {string.Join(", ", createdAtActionResult.RouteValues?.Keys ?? Array.Empty<string>())}");
        
        createdAtActionResult.ActionName.Should().Be(nameof(EventsController.Get));
        createdAtActionResult.RouteValues["id"].Should().Be(createdEventId);
        createdAtActionResult.Value.Should().BeOfType<EventResponse>();
    }
    
    [Fact]
    public async Task Create_Should_ReturnsBadRequest_When_ValidationFails()
    {
        // Arrange
        var invalidRequest = _fixture.Build<CreateEventRequest>()
                                    .With(r => r.Title, "") 
                                    .Create();
        
        var validationFailure = new FluentValidation.Results.ValidationFailure("Title", "Title is required.");
        var validationResult = new FluentValidation.Results.ValidationResult([validationFailure]);
    
        _validator
            .ValidateAsync(Arg.Any<CreateEventRequest>(), CancellationToken.None)
            .Returns(validationResult);
    
        // Act
        var result = await _sut.Create(invalidRequest, CancellationToken.None);
    
        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.Should().BeOfType<ValidationProblemDetails>();
        var problemDetails = (ValidationProblemDetails)badRequestResult.Value;
        problemDetails.Errors.Should().ContainKey("Title");
        problemDetails.Errors["Title"].Should().Contain("Title is required.");
    }

    [Fact]
    public async Task Create_Should_ReturnsConflict_When_TimeConflictOccurs()
    {
        // Arrange
        var request = _fixture.Create<CreateEventRequest>();
        var entity = _fixture.Create<CalendarEvent>();
        _mapper.Map<CalendarEvent>(request).Returns(entity);

        var innerException =
            new Exception("23514: conflicting key value violates exclusion constraint \"no_overlapping_bookings\"");

        var dbUpdateException = new DbUpdateException("Database update failed", innerException);

        _service.CreateEventAsync(Arg.Any<CalendarEvent>(), _endPoint, Arg.Any<CancellationToken>())
            .Throws(dbUpdateException);
        // Act
        var result = await _sut.Create(request, CancellationToken.None);
        
        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
        var conflictResult = (ConflictObjectResult)result;
        conflictResult.StatusCode.Should().Be(409);

        var value = conflictResult.Value.Should().BeAssignableTo<object>().Subject;
        var messageProperty = value.GetType().GetProperty("message");
        messageProperty?.GetValue(value)?.Should().Be("A database error occurred while creating the event.");
    }

    [Fact]
    public async Task Create_Should_ReturnsConflict_When_NotEnoughFunds()
    {
        // Arrange
        var request = _fixture.Create<CreateEventRequest>();
        var entity = _fixture.Create<CalendarEvent>();
        _mapper.Map<CalendarEvent>(request).Returns(entity);

        var invalidOperationException =
            new InvalidOperationException("Not enough money. User 'abc' has 5.0, but 10.0 is required.");

        _service.CreateEventAsync(Arg.Any<CalendarEvent>(), _endPoint, Arg.Any<CancellationToken>())
            .Throws(invalidOperationException);
        
        // Act
        var result = await _sut.Create(request, CancellationToken.None);
        
        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
        var conflictResult = (ConflictObjectResult)result;
        conflictResult.StatusCode.Should().Be(409);
        
        var value = conflictResult.Value.Should().BeAssignableTo<object>().Subject;
        var messageProperty = value.GetType().GetProperty("message");
        messageProperty?.GetValue(value)?.Should().Be("Not enough money. User 'abc' has 5.0, but 10.0 is required.");
    }
#endregion
}