using Microsoft.EntityFrameworkCore;

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
    }
}
