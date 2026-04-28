using Calendar.Auth.API.Controllers;
using Calendar.Auth.API.Data;
using Calendar.Auth.API.Entities;
using Calendar.Auth.API.UnitTests.Helpers;
using Calendar.Contracts.Events;
using Calendar.Contracts.Requests;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Calendar.Auth.API.UnitTests.Controllers;

public class AccountControllerTests : IDisposable
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly AuthDbContext _dbContext;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        _userManager = MockHelpers.MockUserManager<ApplicationUser>();
        _signInManager = MockHelpers.MockSignInManager(_userManager);
        _publishEndpoint = Substitute.For<IPublishEndpoint>();

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _dbContext = new AuthDbContext(options);

        _controller = new AccountController(_signInManager);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsRedirect()
    {
        // Arrange
        string email = "test@example.com";
        string password = "Password123!";
        string returnUrl = "/home";
        
        _signInManager.PasswordSignInAsync(email, password, false, false)
            .Returns(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Act
        var result = await _controller.Login(email, password, returnUrl);

        // Assert
        result.Should().BeOfType<RedirectResult>()
            .Which.Url.Should().Be(returnUrl);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsBadRequest()
    {
        // Arrange
        _signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), false, false)
            .Returns(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var result = await _controller.Login("bad@user.com", "wrong", "/");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be("Неверный логи или пароль");
    }

    [Fact]
    public async Task Register_WhenUserAlreadyExists_ReturnsConflict()
    {
        // Arrange
        var request = new RegisterRequest("existing", "test@test.com", "Pass!");
        _userManager.FindByNameAsync(request.Username).Returns(new ApplicationUser());

        // Act
        var result = await _controller.Register(request, _userManager, _publishEndpoint, _dbContext, default);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Register_WhenSuccessful_CommitsTransactionAndPublishesEvent()
    {
        // Arrange
        var request = new RegisterRequest("newuser", "new@test.com", "SecurePass123!");
        _userManager.FindByNameAsync(request.Username).Returns((ApplicationUser)null!);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), request.Password)
            .Returns(IdentityResult.Success);

        // Act
        var result = await _controller.Register(request, _userManager, _publishEndpoint, _dbContext, default);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        await _userManager.Received(1).CreateAsync(Arg.Is<ApplicationUser>(u => 
            u.UserName == request.Username && u.Email == request.Email), request.Password);
            
        await _publishEndpoint.Received(1).Publish(Arg.Is<UserRegisteredEvent>(e => 
            e.Email == request.Email && e.Name == request.Username), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_WhenCreationFails_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest("bad", "bad@test.com", "123");
        var identityError = new IdentityError { Code = "1", Description = "Error" };
        
        _userManager.FindByNameAsync(request.Username).Returns((ApplicationUser)null!);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), request.Password)
            .Returns(IdentityResult.Failed(identityError));

        // Act
        var result = await _controller.Register(request, _userManager, _publishEndpoint, _dbContext, default);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<UserRegisteredEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var request = new RegisterRequest("ex", "ex@test.com", "Pass!");
        _userManager.FindByNameAsync(request.Username).Returns((ApplicationUser)null!);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Throws(new Exception("DB error"));

        // Act
        var result = await _controller.Register(request, _userManager, _publishEndpoint, _dbContext, default);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<UserRegisteredEvent>(), Arg.Any<CancellationToken>());
    }
}
