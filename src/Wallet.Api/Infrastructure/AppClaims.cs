using System.Security.Claims;

namespace Wallet.Api.Infrastructure;

public static class AppClaims
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Role = "role";

    /// <summary>Id of the authenticated user, taken from the token's "sub" claim.</summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue(Subject);

        return Guid.TryParse(subject, out var userId)
            ? userId
            : throw new InvalidOperationException("The access token does not contain a valid 'sub' claim.");
    }
}
