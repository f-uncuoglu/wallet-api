using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Wallet.Api.Entities;
using Wallet.Api.Infrastructure;
using Wallet.Api.Services;

namespace Wallet.Tests.Unit;

public class JwtTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly JwtOptions JwtSettings = new()
    {
        Issuer = "wallet-api",
        Audience = "wallet-clients",
        Key = "unit-test-signing-key-0123456789abcdef",
        ExpiryMinutes = 30
    };

    private static readonly User Admin = new()
    {
        Email = "admin@example.com",
        FullName = "Admin",
        Role = UserRole.Admin
    };

    private static JwtTokenService CreateService() =>
        new(Options.Create(JwtSettings), new FixedTimeProvider(Now));

    [Fact]
    public void Token_carries_the_user_id_email_and_role()
    {
        var accessToken = CreateService().CreateAccessToken(Admin);

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(accessToken.Value);

        Assert.Equal(Admin.Id.ToString(), jwt.Subject);
        Assert.Equal("admin@example.com", jwt.GetClaim(AppClaims.Email).Value);
        Assert.Equal("Admin", jwt.GetClaim(AppClaims.Role).Value);
        Assert.Equal("wallet-api", jwt.Issuer);
        Assert.Contains("wallet-clients", jwt.Audiences);
    }

    [Fact]
    public void Token_expires_after_the_configured_number_of_minutes()
    {
        var accessToken = CreateService().CreateAccessToken(Admin);

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(accessToken.Value);

        Assert.Equal(Now.AddMinutes(30), accessToken.ExpiresAt);
        Assert.Equal(Now.AddMinutes(30).UtcDateTime, jwt.ValidTo);
    }

    [Fact]
    public void Token_does_not_contain_the_password_hash()
    {
        var user = new User { Email = "a@example.com", FullName = "A", PasswordHash = "super-secret-hash" };

        var accessToken = CreateService().CreateAccessToken(user);

        // The payload of a JWT is only base64url-encoded, not encrypted: anyone can read it.
        var payload = accessToken.Value.Split('.')[1];
        var json = System.Text.Encoding.UTF8.GetString(Base64UrlDecode(payload));

        Assert.DoesNotContain("super-secret-hash", json);
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
        return Convert.FromBase64String(base64);
    }
}
