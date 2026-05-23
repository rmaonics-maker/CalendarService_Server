using System.Security.Claims;
using AutoFixture;
using AutoMapper;
using Calendar_Service.Controllers;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Requests;
using Calendar.Contracts.Responses;
using Calendar_Service.Services;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OpenIddict.Abstractions;
using Xunit.Abstractions;

namespace Calendar.UnitTests.Controllers;

public class AppointmentsControllerTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly AppointmentsController _sut;
    private readonly IAppointmentService _service;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateAppointmentRequest> _validator;
    private readonly IRecurrenceEvaluator _recurrenceEvaluator;
    private readonly IFixture _fixture;

    public AppointmentsControllerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _service = Substitute.For<IAppointmentService>();
        _mapper = Substitute.For<IMapper>();
        _validator = Substitute.For<IValidator<CreateAppointmentRequest>>();
        _recurrenceEvaluator = Substitute.For<IRecurrenceEvaluator>();
        _fixture = new Fixture();

        _validator.ValidateAsync(Arg.Any<CreateAppointmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        
        _sut = new AppointmentsController(_service, _mapper, _validator, _recurrenceEvaluator);
        
        // Setup User Claim
        var userId = Guid.NewGuid().ToString();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(OpenIddictConstants.Claims.Subject, userId)
        }, "mock"));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }
    
#region Create
    [Fact]
    public async Task Create_Should_ReturnsCreatedAtAction_When_ValidRequest()
    {
        // Arrange
        var request = _fixture.Create<CreateAppointmentRequest>();
        var dto = _fixture.Create<AppointmentDto>();
        _mapper.Map<AppointmentDto>(request).Returns(dto);

        _service.CreateAppointmentAsync(Arg.Any<AppointmentDto>(), Arg.Any<CancellationToken>())
            .Returns(dto);

        var response = _fixture.Create<AppointmentResponse>();
        _mapper.Map<AppointmentResponse>(Arg.Any<AppointmentDto>()).Returns(response);
        
        // Act
        var result = await _sut.CreateAppointment(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdAtActionResult = (CreatedAtActionResult)result;
        
        createdAtActionResult.ActionName.Should().Be(nameof(AppointmentsController.GetAppointment));
        createdAtActionResult.RouteValues["id"].Should().Be(dto.Id);
        createdAtActionResult.Value.Should().BeOfType<AppointmentResponse>();
    }
    
    [Fact]
    public async Task Create_Should_ReturnsValidationProblem_When_ValidationFails()
    {
        // Arrange
        var invalidRequest = _fixture.Create<CreateAppointmentRequest>();
        
        var validationFailure = new FluentValidation.Results.ValidationFailure("Title", "Title is required.");
        var validationResult = new FluentValidation.Results.ValidationResult([validationFailure]);
    
        _validator
            .ValidateAsync(Arg.Any<CreateAppointmentRequest>(), CancellationToken.None)
            .Returns(validationResult);
    
        // Act
        var result = await _sut.CreateAppointment(invalidRequest, CancellationToken.None);
    
        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.Should().BeOfType<ValidationProblemDetails>();
    }

    [Fact]
    public async Task Create_Should_ReturnsConflict_When_DbUpdateException()
    {
        // Arrange
        var request = _fixture.Create<CreateAppointmentRequest>();
        var dto = _fixture.Create<AppointmentDto>();
        _mapper.Map<AppointmentDto>(request).Returns(dto);

        var dbUpdateException = new DbUpdateException("Database update failed", new Exception("Conflict"));

        _service.CreateAppointmentAsync(Arg.Any<AppointmentDto>(), Arg.Any<CancellationToken>())
            .Throws(dbUpdateException);
        
        // Act
        var result = await _sut.CreateAppointment(request, CancellationToken.None);
        
        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
        var conflictResult = (ConflictObjectResult)result;
        conflictResult.StatusCode.Should().Be(409);
    }
#endregion

#region Get
    [Fact]
    public async Task Get_Should_ReturnsNotFound_When_EventNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _service.GetAppointmentAsync(id, Arg.Any<CancellationToken>()).Returns((AppointmentDto?)null);

        //Act
        var result = await _sut.GetAppointment(id.ToString());

        //Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Get_Should_ReturnsOk_When_GetEvent()
    {
        // Arrange
        var dto = _fixture.Create<AppointmentDto>();
        dto = dto with { Id = Guid.NewGuid().ToString() }; // Ensure it's a valid Guid string
        var response = _fixture.Create<AppointmentResponse>();
        _service.GetAppointmentAsync(Guid.Parse(dto.Id), Arg.Any<CancellationToken>()).Returns(dto);
        _mapper.Map<AppointmentResponse>(dto).Returns(response);
        
        // Act
        var result = await _sut.GetAppointment(dto.Id);
        
        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().Be(response);
    }
#endregion

#region GetAppointments
    [Fact]
    public async Task GetAppointments_Should_ReturnsBadRequest_When_RangeMoreOneYear()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow;
        var to = from.AddDays(366);
        
        // Act
        var result = await _sut.GetAppointments(from, to);
        
        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetAppointments_Should_ReturnsOk_When_ValidRange()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow;
        var to = from.AddDays(7);
        var dtos = _fixture.CreateMany<AppointmentDto>(2);
        var responses = _fixture.CreateMany<AppointmentResponse>(2);
        
        _service.GetAppointmentsAsync(from, to, Arg.Any<CancellationToken>()).Returns(dtos);
        _mapper.Map<IEnumerable<AppointmentResponse>>(dtos).Returns(responses);
        
        // Act
        var result = await _sut.GetAppointments(from, to);
        
        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().Be(responses);
    }
#endregion

#region Delete
    [Fact]
    public async Task Delete_Should_ReturnNotFound_When_EventNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _service.DeleteAppointmentAsync(id, Arg.Any<CancellationToken>()).Returns(false);
        
        // Act
        var result = await _sut.DeleteAppointment(id, CancellationToken.None);
        
        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_Should_ReturnNoContent_When_EventFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _service.DeleteAppointmentAsync(id, Arg.Any<CancellationToken>()).Returns(true);
        
        // Act
        var result = await _sut.DeleteAppointment(id, CancellationToken.None);
        
        // Assert
        result.Should().BeOfType<NoContentResult>();
    }
#endregion
    
#region Update

    [Fact]
    public async Task Update_Should_ReturnsNotFound_When_EventNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = _fixture.Create<UpdateAppointmentRequest>();
        _service.GetAppointmentAsync(id, Arg.Any<CancellationToken>()).Returns((AppointmentDto?)null);
        
        // Act
        var result = await _sut.UpdateAppointment(id, request, CancellationToken.None);
        
        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
    
    [Fact]
    public async Task Update_Should_ReturnsCreatedAtAction_When_UpdateSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = _fixture.Create<UpdateAppointmentRequest>();
        
        // Mock User ID to match DTO's UserID
        var userId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(OpenIddictConstants.Claims.Subject, userId.ToString())
        }, "mock"));
        _sut.ControllerContext.HttpContext.User = user;

        var existingDto = _fixture.Build<AppointmentDto>().With(x => x.UserId, userId).Create();
        var updatedDto = _fixture.Create<AppointmentDto>();
        var response = _fixture.Create<AppointmentResponse>();

        _service.GetAppointmentAsync(id, Arg.Any<CancellationToken>()).Returns(existingDto);
        _mapper.Map<AppointmentDto>(request).Returns(updatedDto);
        _service.UpdateAppointmentAsync(id, updatedDto, Arg.Any<CancellationToken>()).Returns(updatedDto);
        _mapper.Map<AppointmentResponse>(updatedDto).Returns(response);
        
        // Act
        var result = await _sut.UpdateAppointment(id, request, CancellationToken.None);
        
        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }
#endregion
}
