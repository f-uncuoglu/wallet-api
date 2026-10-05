using Wallet.Api.Dtos;

namespace Wallet.Api.Services;

public interface IAuthService
{
    Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
