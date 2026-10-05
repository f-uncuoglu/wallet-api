namespace Wallet.Api.Entities;

public enum UserRole
{
    User,
    Admin
}

public class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Stored trimmed and lower-cased; unique.</summary>
    public required string Email { get; set; }

    public required string FullName { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.User;

    public DateTimeOffset CreatedAt { get; set; }
}
