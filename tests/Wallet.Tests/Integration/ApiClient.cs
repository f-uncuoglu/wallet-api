using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Wallet.Api.Controllers;
using Wallet.Api.Data;
using Wallet.Api.Dtos;
using Wallet.Api.Entities;

namespace Wallet.Tests.Integration;

/// <summary>Small helpers so the tests read like the scenario they describe.</summary>
public static class ApiClient
{
    public const string Password = "Passw0rd!";

    /// <summary>Same JSON conventions as the API: camelCase and enums as strings.</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    /// <summary>Registers a brand-new user and returns a client that sends their access token.</summary>
    public static async Task<HttpClient> CreateUserClientAsync(this WalletApiFactory factory)
    {
        var client = factory.CreateClient();
        var email = UniqueEmail();

        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, Password, "Test User"), Json);
        register.EnsureSuccessStatusCode();

        await client.LoginAsync(email, Password);
        return client;
    }

    /// <summary>Inserts an admin directly into the database (there is no public way to become one).</summary>
    public static async Task<HttpClient> CreateAdminClientAsync(this WalletApiFactory factory)
    {
        var email = UniqueEmail();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

            var admin = new User
            {
                Email = email,
                FullName = "Test Admin",
                Role = UserRole.Admin,
                CreatedAt = DateTimeOffset.UtcNow
            };
            admin.PasswordHash = hasher.HashPassword(admin, Password);

            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        await client.LoginAsync(email, Password);
        return client;
    }

    public static async Task LoginAsync(this HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password), Json);
        response.EnsureSuccessStatusCode();

        var token = await response.ReadAsync<TokenResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
    }

    public static async Task<AccountResponse> CreateAccountAsync(
        this HttpClient client,
        decimal balance = 0m,
        Currency currency = Currency.TRY)
    {
        var response = await client.PostAsJsonAsync("/api/accounts", new CreateAccountRequest("Test account", currency), Json);
        response.EnsureSuccessStatusCode();
        var account = await response.ReadAsync<AccountResponse>();

        if (balance > 0)
        {
            var deposit = await client.DepositAsync(account.Id, balance);
            deposit.EnsureSuccessStatusCode();
        }

        return account;
    }

    public static Task<HttpResponseMessage> DepositAsync(this HttpClient client, Guid accountId, decimal amount) =>
        client.PostAsJsonAsync($"/api/accounts/{accountId}/deposits", new DepositRequest(amount, "test deposit"), Json);

    public static Task<HttpResponseMessage> WithdrawAsync(this HttpClient client, Guid accountId, decimal amount) =>
        client.PostAsJsonAsync($"/api/accounts/{accountId}/withdrawals", new WithdrawRequest(amount, "test withdrawal"), Json);

    public static async Task<decimal> GetBalanceAsync(this HttpClient client, Guid accountId)
    {
        var account = await client.GetFromJsonAsync<AccountResponse>($"/api/accounts/{accountId}", Json);
        return account!.Balance;
    }

    /// <summary>Sends a transfer. A fresh idempotency key is generated unless one is given.</summary>
    public static Task<HttpResponseMessage> TransferAsync(
        this HttpClient client,
        Guid from,
        Guid to,
        decimal amount,
        string? idempotencyKey = null,
        bool omitIdempotencyKey = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/transfers")
        {
            Content = JsonContent.Create(new CreateTransferRequest(from, to, amount, "test transfer"), options: Json)
        };

        if (!omitIdempotencyKey)
        {
            request.Headers.Add(TransfersController.IdempotencyKeyHeader, idempotencyKey ?? Guid.NewGuid().ToString());
        }

        return client.SendAsync(request);
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(Json);
        return value ?? throw new InvalidOperationException($"Response body could not be read as {typeof(T).Name}.");
    }
}
