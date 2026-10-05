using Wallet.Api.Entities;

namespace Wallet.Api.Services;

public record AccessToken(string Value, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    AccessToken CreateAccessToken(User user);
}
