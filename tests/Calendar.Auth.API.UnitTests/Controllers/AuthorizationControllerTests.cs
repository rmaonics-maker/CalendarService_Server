using System.Security.Claims;
using Calendar.Auth.API.Controllers;
using Calendar.Auth.API.Entities;
using Calendar.Auth.API.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Xunit;

namespace Calendar.Auth.API.UnitTests.Controllers;

public class AuthorizationControllerTests
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AuthorizationController _controller;

    public AuthorizationControllerTests()
    {
        _userManager = MockHelpers.MockUserManager<ApplicationUser>();
        _controller = new AuthorizationController(_userManager);
    }

    [Fact]
    public async Task Authorize_WhenNotAuthenticated_ReturnsRedirectToLogin()
    {
        // Arrange
        var request = new OpenIddictRequest();
        _controller.ControllerContext.HttpContext = MockHelpers.MockHttpContext(principal: null, openIddictRequest: request);
        _controller.ControllerContext.HttpContext.Request.PathBase = "";
        _controller.ControllerContext.HttpContext.Request.Path = "/connect/authorize";
        _controller.ControllerContext.HttpContext.Request.QueryString = new QueryString("?client_id=test");

        // Act
        var result = await _controller.Authorize();

        // Assert
        result.Should().BeOfType<RedirectResult>()
            .Which.Url.Should().Contain("/Account/Login");
    }

    [Fact]
    public async Task Authorize_WhenAuthenticated_ReturnsSignInResult()
    {
        // Arrange
        var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), Email = "test@test.com" };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, user.Id)], 
            IdentityConstants.ApplicationScheme));
        
        var request = new OpenIddictRequest();
        request.Scope = "offline_access profile";
        
        _controller.ControllerContext.HttpContext = MockHelpers.MockHttpContext(principal: principal, openIddictRequest: request);
        _userManager.GetUserAsync(principal).Returns(user);
        _userManager.GetRolesAsync(user).Returns(["Admin"]);

        // Act
        var result = await _controller.Authorize();

        // Assert
        var signInResult = result.Should().BeOfType<Microsoft.AspNetCore.Mvc.SignInResult>().Subject;
        signInResult.AuthenticationScheme.Should().Be(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        signInResult.Principal.HasClaim(OpenIddictConstants.Claims.Subject, user.Id).Should().BeTrue();
        signInResult.Principal.HasClaim(OpenIddictConstants.Claims.Name, user.Email).Should().BeTrue();
        signInResult.Principal.HasClaim(OpenIddictConstants.Claims.Role, "Admin").Should().BeTrue();
    }

    [Fact]
    public async Task Authorize_WhenNoOpenIddictRequest_ThrowsInvalidOperationException()
    {
        // Arrange
        _controller.ControllerContext.HttpContext = new DefaultHttpContext();

        // Act
        var action = () => _controller.Authorize();

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
    }
}
