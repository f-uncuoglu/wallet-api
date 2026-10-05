namespace Wallet.Api.Infrastructure;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "wallet-api";

    public string Audience { get; set; } = "wallet-clients";

    /// <summary>
    /// Symmetric signing key (HMAC-SHA256). Must come from configuration: user secrets or
    /// an environment variable (Jwt__Key) outside of local development.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 60;
}
