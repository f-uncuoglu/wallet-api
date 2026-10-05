using System.Net;
using System.Net.Http.Json;
using Wallet.Api.Dtos;
using Wallet.Api.Entities;

namespace Wallet.Tests.Integration;

[Collection(ApiCollection.Name)]
public class TransactionHistoryTests(WalletApiFactory factory)
{
    private static Task<PagedResult<TransactionResponse>?> GetHistoryAsync(HttpClient client, Guid accountId, string query = "") =>
        client.GetFromJsonAsync<PagedResult<TransactionResponse>>($"/api/accounts/{accountId}/transactions{query}", ApiClient.Json);

    [Fact]
    public async Task History_is_paged_and_ordered_newest_first()
    {
        var client = await factory.CreateUserClientAsync();
        var account = await client.CreateAccountAsync();
        for (var amount = 1; amount <= 5; amount++)
        {
            (await client.DepositAsync(account.Id, amount)).EnsureSuccessStatusCode();
        }

        var firstPage = await GetHistoryAsync(client, account.Id, "?page=1&pageSize=2");
        var lastPage = await GetHistoryAsync(client, account.Id, "?page=3&pageSize=2");

        Assert.Equal(5, firstPage!.TotalCount);
        Assert.Equal(3, firstPage.TotalPages);
        Assert.True(firstPage.HasNextPage);
        Assert.Equal(new[] { 5m, 4m }, firstPage.Items.Select(t => t.Amount));
        Assert.Equal(new[] { 1m }, lastPage!.Items.Select(t => t.Amount));
        Assert.False(lastPage.HasNextPage);
    }

    [Fact]
    public async Task History_can_be_filtered_by_type()
    {
        var client = await factory.CreateUserClientAsync();
        var account = await client.CreateAccountAsync(balance: 100m);
        (await client.WithdrawAsync(account.Id, 20m)).EnsureSuccessStatusCode();
        (await client.WithdrawAsync(account.Id, 30m)).EnsureSuccessStatusCode();

        var withdrawals = await GetHistoryAsync(client, account.Id, "?type=Withdrawal");

        Assert.Equal(2, withdrawals!.TotalCount);
        Assert.All(withdrawals.Items, t => Assert.Equal(TransactionType.Withdrawal, t.Type));
    }

    [Fact]
    public async Task History_can_be_filtered_by_date_range()
    {
        var client = await factory.CreateUserClientAsync();
        var account = await client.CreateAccountAsync(balance: 100m);

        var tomorrow = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(1).ToString("O"));
        var yesterday = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(-1).ToString("O"));

        var future = await GetHistoryAsync(client, account.Id, $"?from={tomorrow}");
        var recent = await GetHistoryAsync(client, account.Id, $"?from={yesterday}&to={tomorrow}");

        Assert.Empty(future!.Items);
        Assert.Single(recent!.Items);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?type=Bribe")]
    public async Task Invalid_query_returns_400(string query)
    {
        var client = await factory.CreateUserClientAsync();
        var account = await client.CreateAccountAsync();

        var response = await client.GetAsync($"/api/accounts/{account.Id}/transactions{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task History_of_another_users_account_is_not_found()
    {
        var alice = await factory.CreateUserClientAsync();
        var bob = await factory.CreateUserClientAsync();
        var account = await alice.CreateAccountAsync(balance: 10m);

        var response = await bob.GetAsync($"/api/accounts/{account.Id}/transactions");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
