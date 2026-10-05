using System.Net;
using System.Net.Http.Json;
using Wallet.Api.Dtos;
using Wallet.Api.Entities;

namespace Wallet.Tests.Integration;

[Collection(ApiCollection.Name)]
public class AccountTests(WalletApiFactory factory)
{
    [Fact]
    public async Task Create_returns_201_with_a_location_header_and_zero_balance()
    {
        var client = await factory.CreateUserClientAsync();

        var response = await client.PostAsJsonAsync("/api/accounts", new CreateAccountRequest("Savings", Currency.USD), ApiClient.Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var account = await response.ReadAsync<AccountResponse>();
        Assert.Equal("Savings", account.Name);
        Assert.Equal(Currency.USD, account.Currency);
        Assert.Equal(0m, account.Balance);
        Assert.EndsWith($"/api/accounts/{account.Id}", response.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Balance_cannot_be_set_by_the_client()
    {
        var client = await factory.CreateUserClientAsync();

        // Over-posting attempt: the request DTO has no Balance, so the extra field is ignored.
        var response = await client.PostAsJsonAsync("/api/accounts",
            new { name = "Hacked", currency = "TRY", balance = 1_000_000 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(0m, (await response.ReadAsync<AccountResponse>()).Balance);
    }

    [Fact]
    public async Task List_only_contains_the_accounts_of_the_caller()
    {
        var alice = await factory.CreateUserClientAsync();
        var bob = await factory.CreateUserClientAsync();
        var aliceAccount = await alice.CreateAccountAsync();
        await bob.CreateAccountAsync();

        var accounts = await alice.GetFromJsonAsync<List<AccountResponse>>("/api/accounts", ApiClient.Json);

        Assert.Equal(aliceAccount.Id, Assert.Single(accounts!).Id);
    }

    [Fact]
    public async Task Account_of_another_user_is_reported_as_not_found()
    {
        var alice = await factory.CreateUserClientAsync();
        var bob = await factory.CreateUserClientAsync();
        var aliceAccount = await alice.CreateAccountAsync(balance: 100m);

        var read = await bob.GetAsync($"/api/accounts/{aliceAccount.Id}");
        var withdraw = await bob.WithdrawAsync(aliceAccount.Id, 50m);

        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, withdraw.StatusCode);
        Assert.Equal(100m, await alice.GetBalanceAsync(aliceAccount.Id));
    }

    [Fact]
    public async Task Deposit_and_withdrawal_change_the_balance_and_are_recorded()
    {
        var client = await factory.CreateUserClientAsync();
        var account = await client.CreateAccountAsync();

        var deposit = await (await client.DepositAsync(account.Id, 100m)).ReadAsync<TransactionResponse>();
        var withdrawal = await (await client.WithdrawAsync(account.Id, 30.25m)).ReadAsync<TransactionResponse>();

        Assert.Equal(TransactionType.Deposit, deposit.Type);
        Assert.Equal(100m, deposit.BalanceAfter);
        Assert.Equal(TransactionType.Withdrawal, withdrawal.Type);
        Assert.Equal(69.75m, withdrawal.BalanceAfter);
        Assert.Equal(69.75m, await client.GetBalanceAsync(account.Id));
    }

    [Fact]
    public async Task Withdrawing_more_than_the_balance_returns_422_and_changes_nothing()
    {
        var client = await factory.CreateUserClientAsync();
        var account = await client.CreateAccountAsync(balance: 50m);

        var response = await client.WithdrawAsync(account.Id, 50.01m);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(50m, await client.GetBalanceAsync(account.Id));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-10")]
    [InlineData("10.001")]
    public async Task Deposit_with_an_invalid_amount_returns_400(string amount)
    {
        var client = await factory.CreateUserClientAsync();
        var account = await client.CreateAccountAsync();

        var response = await client.DepositAsync(account.Id, decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0m, await client.GetBalanceAsync(account.Id));
    }

    [Fact]
    public async Task Concurrent_deposits_do_not_lose_updates()
    {
        var client = await factory.CreateUserClientAsync();
        var account = await client.CreateAccountAsync();

        // Ten requests read the same balance at the same time. Without the row version check
        // most of them would overwrite each other ("lost update").
        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => client.DepositAsync(account.Id, 10m)));

        var succeeded = responses.Count(r => r.IsSuccessStatusCode);
        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode || r.StatusCode == HttpStatusCode.Conflict));
        Assert.True(succeeded > 0);
        Assert.Equal(succeeded * 10m, await client.GetBalanceAsync(account.Id));
    }
}
