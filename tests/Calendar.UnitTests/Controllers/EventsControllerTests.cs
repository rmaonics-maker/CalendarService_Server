using AutoFixture;
using AutoMapper;
using Calendar_Service.Controllers;
using Calendar_Service.Models;
using Calendar_Service.Services;
using Calendar.Contracts;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Calendar.UnitTests.Controllers;

public class EventsControllerTests
{
    private readonly EventsController _sut;
    private readonly ICalendarService _service;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateEventRequest> _validator;
    private readonly IPublishEndpoint _endPoint;
    private readonly IFixture _fixture;

    public EventsControllerTests()
    {
        _service = Substitute.For<ICalendarService>();
        _mapper = Substitute.For<IMapper>();
        _validator = Substitute.For<IValidator<CreateEventRequest>>();
        _endPoint = Substitute.For<IPublishEndpoint>();
        _fixture = new Fixture();

        _validator.ValidateAsync(Arg.Any<ValidationContext<CreateEventRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        
        _sut = new EventsController(_service, _mapper, _validator, _endPoint);
    }

    [Fact]
    public async Task Create_Should_ReturnsCreatedAtAction_When_ValidRequest()
    {
        // Arrange
        var request = _fixture.Create<CreateEventRequest>();
        var createdEventId = Guid.NewGuid();
        var userEmail = _fixture.Create<string>();
        
        _service.CreateEventAsync(Arg.Any<CalendarEvent>(), _endPoint, Arg.Any<CancellationToken>())
            .Returns((createdEventId, userEmail));
        
        _mapper.Map<CalendarEvent>(request).Returns(Arg.Any<CalendarEvent>());
        
        // Act
        var result = await _sut.Create(request, Arg.Any<CancellationToken>());

        // Assert
        /*result.Should().BeOfType<CreatedAtActionResult>();
        var createdAtActionResult = (CreatedAtActionResult)result;
        createdAtActionResult.ActionName.Should().Be(nameof(EventsController.Get));
        createdAtActionResult.RouteValues["id"].Should().Be(createdEventId);
        createdAtActionResult.Value.Should().BeOfType<EventResponse>();*/
    }
    
    [Fact]
    public async Task Create_WhenValidationFails_ReturnsBadRequestWithValidationProblemDetails()
    {
        // Arrange
        var invalidRequest = _fixture.Build<CreateEventRequest>()
                                          .With(r => r.Title, "") // Делаем заголовок невалидным
                                          .Create();
    
        // Настраиваем мок валидатора, чтобы он возвращал ошибки
        var validationFailure = new FluentValidation.Results.ValidationFailure("Title", "Title is required.");
        var validationResult = new FluentValidation.Results.ValidationResult(new[] { validationFailure });
    
        _validator
                 .ValidateAsync(Arg.Any<ValidationContext<CreateEventRequest>>(), Arg.Any<CancellationToken>())
                 .Returns(validationResult);
    
        // Act
        var result = await _sut.Create(invalidRequest, Arg.Any<CancellationToken>());
    
        // Assert
        /*result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.Should().BeOfType<ValidationProblemDetails>();
        var problemDetails = (ValidationProblemDetails)badRequestResult.Value;
        problemDetails.Errors.Should().ContainKey("Title");
        problemDetails.Errors["Title"].Should().Contain("Title is required.");*/
    }
}