using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Wallet.Api.Data;
using Wallet.Api.Dtos;
using Wallet.Api.Entities;
using Wallet.Api.Exceptions;

namespace Wallet.Api.Services;

public sealed class AuthService(
    WalletDbContext db,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService,
    TimeProvider timeProvider) : IAuthService
{
    public async Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);

        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            throw new ConflictException("An account with this email address already exists.");
        }

        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            CreatedAt = timeProvider.GetUtcNow()
        };

        // PBKDF2 with a per-user salt; the plain password is never stored or logged.
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            // Two registrations with the same email raced past the check above;
            // the unique index on Email decided the winner.
            throw new ConflictException("An account with this email address already exists.");
        }

        return new UserResponse(user.Id, user.Email, user.FullName, user.Role, user.CreatedAt);
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

        // Same error for "unknown email" and "wrong password", so the endpoint
        // cannot be used to find out which emails are registered.
        if (user is null)
        {
            throw new InvalidCredentialsException();
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            throw new InvalidCredentialsException();
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            await db.SaveChangesAsync(cancellationToken);
        }

        var token = tokenService.CreateAccessToken(user);

        return new TokenResponse(token.Value, "Bearer", token.ExpiresAt);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
