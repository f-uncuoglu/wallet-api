using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wallet.Api.Data;

namespace Wallet.Tests.Integration;

/// <summary>
/// Boots the real application in memory (same Program.cs, same middleware pipeline, same DI
/// container) against a real PostgreSQL database, so the tests exercise actual SQL,
/// transactions and row versions instead of an in-memory fake.
///
/// The database is taken from the WALLET_TEST_DB environment variable; by default it is the
/// PostgreSQL instance started by "docker compose up -d db".
/// </summary>
public sealed class WalletApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=wallet_test;Username=postgres;Password=postgres";

    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("WALLET_TEST_DB") ?? DefaultConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = ConnectionString,
            ["Database:MigrateOnStartup"] = "false",
            ["Jwt:Issuer"] = "wallet-api-tests",
            ["Jwt:Audience"] = "wallet-api-tests",
            ["Jwt:Key"] = "integration-test-signing-key-0123456789abcdef",
            ["Jwt:ExpiryMinutes"] = "15"
        };

        builder.UseEnvironment("Testing");

        foreach (var (key, value) in settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));
    }

    /// <summary>Every test run starts from an empty schema built from the current EF Core model.</summary>
    public async Task InitializeAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WalletDbContext>();

        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();
}

/// <summary>All integration tests share one application instance and one database.</summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<WalletApiFactory>
{
    public const string Name = "Wallet API";
}
