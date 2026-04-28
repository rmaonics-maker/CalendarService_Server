using System.Runtime.Serialization;
using System.Security.Claims;
using Calendar.Auth.API.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;

namespace Calendar.Auth.API.UnitTests.Helpers;

public static class MockHelpers
{
    public static UserManager<TUser> MockUserManager<TUser>() where TUser : class
    {
        var store = Substitute.For<IUserStore<TUser>>();
        var mgr = Substitute.For<UserManager<TUser>>(store, null, null, null, null, null, null, null, null);
        return mgr;
    }

    public static SignInManager<TUser> MockSignInManager<TUser>(UserManager<TUser>? userManager = null) where TUser : class
    {
        var contextAccessor = Substitute.For<IHttpContextAccessor>();
        var claimsFactory = Substitute.For<IUserClaimsPrincipalFactory<TUser>>();
        var options = Substitute.For<IOptions<IdentityOptions>>();
        var logger = Substitute.For<ILogger<SignInManager<TUser>>>();
        var schemes = Substitute.For<IAuthenticationSchemeProvider>();
        var confirmation = Substitute.For<IUserConfirmation<TUser>>();

        return Substitute.For<SignInManager<TUser>>(
            userManager ?? MockUserManager<TUser>(),
            contextAccessor,
            claimsFactory,
            options,
            logger,
            schemes,
            confirmation);
    }

    public static HttpContext MockHttpContext(
        ClaimsPrincipal? principal = null, 
        OpenIddictRequest? openIddictRequest = null)
    {
        var context = new DefaultHttpContext();
        
        if (principal != null)
        {
            var authService = Substitute.For<IAuthenticationService>();
            authService.AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string>())
                .Returns(AuthenticateResult.Success(new AuthenticationTicket(principal, IdentityConstants.ApplicationScheme)));
            
            var serviceProvider = new ServiceCollection()
                .AddSingleton(authService)
                .BuildServiceProvider();
            
            context.RequestServices = serviceProvider;
        }
        else
        {
            var authService = Substitute.For<IAuthenticationService>();
            authService.AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string>())
                .Returns(AuthenticateResult.Fail("Not authenticated"));
            
            var serviceProvider = new ServiceCollection()
                .AddSingleton(authService)
                .BuildServiceProvider();
            
            context.RequestServices = serviceProvider;
        }

        if (openIddictRequest != null)
        {
            var transaction = new OpenIddictServerTransaction
            {
                Request = openIddictRequest
            };
            
            // OpenIddictServerAspNetCoreFeature is sealed and has no public constructor/setter in some versions.
            // We use reflection to bypass this.
            var feature = (OpenIddictServerAspNetCoreFeature)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(OpenIddictServerAspNetCoreFeature));
            
            var transactionField = typeof(OpenIddictServerAspNetCoreFeature)
                .GetField("<Transaction>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            transactionField?.SetValue(feature, transaction);
            
            context.Features.Set(feature);
        }

        return context;
    }
}
