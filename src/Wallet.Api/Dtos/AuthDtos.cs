using Wallet.Api.Entities;

namespace Wallet.Api.Dtos;

public record RegisterRequest(string Email, string Password, string FullName);

public record LoginRequest(string Email, string Password);

public record UserResponse(Guid Id, string Email, string FullName, UserRole Role, DateTimeOffset CreatedAt);

public record TokenResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAt);
