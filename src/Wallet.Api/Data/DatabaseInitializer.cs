using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Wallet.Api.Entities;

namespace Wallet.Api.Data;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies pending EF Core migrations when Database:MigrateOnStartup is true.
    /// Convenient for local development and docker compose; in a real deployment
    /// migrations would run as a separate, controlled step.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WalletDbContext>();

        await db.Database.MigrateAsync();
        await SeedAdminAsync(scope.ServiceProvider, db, app.Configuration);
    }

    /// <summary>
    /// Creates the admin user described by Seed:AdminEmail / Seed:AdminPassword if it
    /// does not exist yet. Does nothing when those settings are missing.
    /// </summary>
    private static async Task SeedAdminAsync(IServiceProvider services, WalletDbContext db, IConfiguration configuration)
    {
        var email = configuration["Seed:AdminEmail"]?.Trim().ToLowerInvariant();
        var password = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            return;
        }

        var admin = new User
        {
            Email = email,
            FullName = "Administrator",
            Role = UserRole.Admin,
            CreatedAt = services.GetRequiredService<TimeProvider>().GetUtcNow()
        };
        admin.PasswordHash = services.GetRequiredService<IPasswordHasher<User>>().HashPassword(admin, password);

        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }
}
