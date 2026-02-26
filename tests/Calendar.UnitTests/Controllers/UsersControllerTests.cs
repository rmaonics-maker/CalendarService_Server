using AutoFixture;
using AutoMapper;
using Calendar_Service.Controllers;
using Calendar_Service.Models;
using Calendar_Service.Services;
using Calendar.Contracts;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Xunit.Abstractions;

namespace Calendar.UnitTests.Controllers;

public class UsersControllerTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ICalendarService _service;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateUserRequest> _validator;
    private readonly IFixture _fixture;
    private readonly UsersController _sut;

    public UsersControllerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _fixture = new Fixture();
        _service = Substitute.For<ICalendarService>();
        _mapper = Substitute.For<IMapper>();
        _validator = Substitute.For<IValidator<CreateUserRequest>>();
        _validator.ValidateAsync(Arg.Any<CreateUserRequest>(), CancellationToken.None)
            .Returns(new ValidationResult());
        
        _sut = new UsersController(_service, _mapper, _validator);
    }

    [Fact]
    public async Task Create_Should_ReturnsValidationProblem_When_ValidationNotValid()
    {
        // Arrange
        var request = _fixture.Create<CreateUserRequest>();
        var validationResult = new ValidationResult();
        validationResult.Errors.Add(new ValidationFailure("Email", "Email is required"));
        _validator.ValidateAsync(request, CancellationToken.None).Returns(validationResult);
        
        // Act
        var result = await _sut.Create(request, CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(400);

        var problem = badRequest.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Errors.Should().HaveCount(1);
        problem.Errors.Should().Contain(e => e.Key == "Email" && e.Value.AsEnumerable().Contains("Email is required"));

        await _service.DidNotReceive().CreateUserAsync(Arg.Any<User>(), CancellationToken.None);
    }

    [Fact]
    public async Task Create_Should_ReturnsConflict_When_InvalidOperation()
    {
        // Arrange
        var request = _fixture.Create<CreateUserRequest>();
        var user = _fixture.Build<User>().Without(x=>x.Bookings).Create();
        _mapper.Map<User>(request).Returns(user);
        var errorMessage = "User with this email already exists";
        _service.CreateUserAsync(user, CancellationToken.None).Throws(new InvalidOperationException(errorMessage));
        
        // Act
        var result = await _sut.Create(request, CancellationToken.None);

        // Assert
        var conflict = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.StatusCode.Should().Be(409);

        var error = conflict.Value;
        var messageProperty = error?.GetType().GetProperty("message");
        messageProperty?.GetValue(error).Should().Be(errorMessage);
        
        await _service.Received(1).CreateUserAsync(user, CancellationToken.None);
    }

    [Fact]
    public async Task Create_Should_ReturnsCreated_When_CreateUser()
    {
        // Arrange
        var request = _fixture.Create<CreateUserRequest>();
        var user = _fixture.Build<User>().Without(x=>x.Bookings).Create();
        _mapper.Map<User>(request).Returns(user);
        _mapper.Map<UserResponse>(user)
            .Returns(new UserResponse { Id = user.Id, Email = user.Email, Name = user.Name });
        // Act
        var result = await _sut.Create(request, CancellationToken.None);

        // Assert
        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.RouteValues["id"].Should().Be(user.Id);
        created.ActionName.Should().Be(nameof(UsersController.Create));
        created.Value.Should().BeOfType<UserResponse>();

        var response = (UserResponse)created.Value;
        response.Id.Should().Be(user.Id);
        response.Name.Should().Be(user.Name);
        response.Email.Should().Be(user.Email);

        await _service.Received(1).CreateUserAsync(user, CancellationToken.None);
    }
    
    [Fact]
    public async Task GetUserHistory_Should_ReturnsNotFound_When_UserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _service.GetUserHistoryAsync(userId, CancellationToken.None).ReturnsNull();
        // Act
        var result = await _sut.GetUserHistory(userId, CancellationToken.None);
        
        // Assert
        var objectResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
        objectResult.Value.Should().Be("User not found.");
    }

    [Fact]
    public async Task GetUserHistory_Should_ReturnsOk_When_UserFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var response = _fixture.Build<UserHistoryResponse>()
            .With(x => x.UserId, userId)
            .Create();
        _service.GetUserHistoryAsync(userId, CancellationToken.None).Returns(response);
        
        // Act
        var result = await _sut.GetUserHistory(userId, CancellationToken.None);
        
        // Assert
        var objectResult = result.Should().BeOfType<OkObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(200);
        objectResult.Value.Should().BeOfType<UserHistoryResponse>();
        var okResponse = (UserHistoryResponse)objectResult.Value;
        okResponse.UserId.Should().Be(userId);
    }
}