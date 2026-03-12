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
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

// 3. перенос данных в JWT         
        var subjectClaim = new Claim(OpenIddictConstants.Claims.Subject, user.Id);
        subjectClaim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
        identity.AddClaim(subjectClaim);

        
        var nameClaim = new Claim(OpenIddictConstants.Claims.Name, user.Email);
        nameClaim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
        identity.AddClaim(nameClaim);

        var audClaim = new Claim(OpenIddictConstants.Claims.Audience, "booking_api");
        audClaim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
        identity.AddClaim(audClaim);

        var principal = new ClaimsPrincipal(identity);
        principal.SetResources("booking_api");
        principal.SetScopes(request.GetScopes());

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
