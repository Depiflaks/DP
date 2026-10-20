using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Valuator.Authentication;

public sealed record UserAccount(string Id, string Login, string PasswordHash)
{
    public ClaimsPrincipal ToPrincipal()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Id),
            new Claim(ClaimTypes.Name, Login)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}
