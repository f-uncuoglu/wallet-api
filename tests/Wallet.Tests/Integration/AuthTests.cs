using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Dtos;
using Wallet.Api.Entities;

namespace Wallet.Tests.Integration;

[Collection(ApiCollection.Name)]
public class AuthTests(WalletApiFactory factory)
{
    [Fact]
    public async Task Register_then_login_returns_an_access_token()
    {
        var client = factory.CreateClient();
        var email = ApiClient.UniqueEmail();

        var register = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email.ToUpperInvariant(), ApiClient.Password, "Ada Lovelace"), ApiClient.Json);

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var user = await register.ReadAsync<UserResponse>();
        Assert.Equal(email, user.Email); // stored lower-cased
        Assert.Equal(UserRole.User, user.Role);

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, ApiClient.Password), ApiClient.Json);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = await login.ReadAsync<TokenResponse>();
        Assert.Equal("Bearer", token.TokenType);
        Assert.False(string.IsNullOrWhiteSpace(token.AccessToken));
        Assert.True(token.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Registering_the_same_email_twice_returns_409()
    {
        var client = factory.CreateClient();
        var request = new RegisterRequest(ApiClient.UniqueEmail(), ApiClient.Password, "Ada Lovelace");

        var first = await client.PostAsJsonAsync("/api/auth/register", request, ApiClient.Json);
        var second = await client.PostAsJsonAsync("/api/auth/register", request, ApiClient.Json);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Registration_with_invalid_data_returns_400_with_field_errors()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("not-an-email", "weak", "Ada Lovelace"), ApiClient.Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsync<ValidationProblemDetails>();
        Assert.Contains(nameof(RegisterRequest.Email), problem.Errors.Keys);
        Assert.Contains(nameof(RegisterRequest.Password), problem.Errors.Keys);
    }

    [Fact]
    public async Task Login_with_a_wrong_password_returns_401()
    {
        var client = factory.CreateClient();
        var email = ApiClient.UniqueEmail();
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, ApiClient.Password, "Ada"), ApiClient.Json);

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Wrong-passw0rd"), ApiClient.Json);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_an_unknown_email_returns_the_same_401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(ApiClient.UniqueEmail(), ApiClient.Password), ApiClient.Json);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_without_a_token_returns_401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/accounts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_with_a_tampered_token_returns_401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJoYWNrZXIifQ.invalid-signature");

        var response = await client.GetAsync("/api/accounts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
