using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Calendar.Auth.API.Entities;
using Microsoft.AspNetCore.Authentication;

namespace Calendar.Auth.API.Controllers;

[ApiController]
public class AuthorizationController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("The OpenID Connect request can not be retrieved.");

// 1. проверка авторизации
        var authenticateResult = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

        if(!authenticateResult.Succeeded)
        {
            var returnUrl = Request.PathBase + Request.Path + Request.QueryString;
            return Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

// 2. выдёргивание пользователя из базы
        var user = await userManager.GetUserAsync(authenticateResult.Principal);

        var principal = await BuildPrincipalAsync(user!, request);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    // Обработка token endpoint. 
    [HttpPost("~/connect/token")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request can not be retrieved.");

        if (request.IsPasswordGrantType())
        {
            var user = await userManager.FindByNameAsync(request.Username!);
            if (user is null || !await userManager.CheckPasswordAsync(user, request.Password!))
            {
                var properties = new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Неверный логин или пароль."
                });
                return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var principal = await BuildPrincipalAsync(user, request);
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsRefreshTokenGrantType())
        {
            var info = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var user = await userManager.GetUserAsync(info.Principal!);
            if (user is null)
            {
                var properties = new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Пользователь не найден."
                });
                return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var principal = await BuildPrincipalAsync(user, request);
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest(new { error = OpenIddictConstants.Errors.UnsupportedGrantType });
    }

    // Общая сборка ClaimsPrincipal для issuance: те же claims, что и в /connect/authorize.
    private async Task<ClaimsPrincipal> BuildPrincipalAsync(ApplicationUser user, OpenIddictRequest request)
    {
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        var subjectClaim = new Claim(OpenIddictConstants.Claims.Subject, user.Id);
        subjectClaim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
        identity.AddClaim(subjectClaim);

        var nameClaim = new Claim(OpenIddictConstants.Claims.Name, user.Email ?? "");
        nameClaim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
        identity.AddClaim(nameClaim);

        foreach (var role in await userManager.GetRolesAsync(user))
        {
            var roleClaim = new Claim(OpenIddictConstants.Claims.Role, role);
            roleClaim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
            identity.AddClaim(roleClaim);
        }

        var principal = new ClaimsPrincipal(identity);
        principal.SetResources("booking_api", "calendar_api");
        principal.SetScopes(request.GetScopes());

        return principal;
    }
}
