using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using Microsoft.AspNetCore;

namespace Calendar.Auth.API.Controllers;

[ApiController]
public class AuthorizationController : ControllerBase
{
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public IActionResult Authorize()
    {
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        
        var subjectClaim = new Claim(OpenIddictConstants.Claims.Subject, "00000000-0000-0000-0000-000000000001");
        
        subjectClaim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
        
        identity.AddClaim(subjectClaim);

        
        var nameClaim = new Claim(OpenIddictConstants.Claims.Name, "Test User");
        nameClaim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
        identity.AddClaim(nameClaim);

        var audClaim = new Claim(OpenIddictConstants.Claims.Audience, "booking_api");
        audClaim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
        identity.AddClaim(audClaim);

        var principal = new ClaimsPrincipal(identity);
        principal.SetResources("booking_api");

        var request = HttpContext.GetOpenIddictServerRequest();
        if (request != null)
        {
            principal.SetScopes(request.GetScopes());
        }

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
