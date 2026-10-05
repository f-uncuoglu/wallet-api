using System.Net;
using System.Net.Http.Json;
using Wallet.Api.Controllers;
using Wallet.Api.Dtos;
using Wallet.Api.Entities;

namespace Wallet.Tests.Integration;

[Collection(ApiCollection.Name)]
public class TransferTests(WalletApiFactory factory)
{
    [Fact]
    public async Task Transfer_moves_the_money_and_writes_a_ledger_entry_on_both_sides()
    {
        var alice = await factory.CreateUserClientAsync();
        var bob = await factory.CreateUserClientAsync();
        var source = await alice.CreateAccountAsync(balance: 100m);
        var destination = await bob.CreateAccountAsync();

        var response = await alice.TransferAsync(source.Id, destination.Id, 40m);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var transfer = await response.ReadAsync<TransferResponse>();
        Assert.Equal(40m, transfer.Amount);
        Assert.Equal(60m, await alice.GetBalanceAsync(source.Id));
        Assert.Equal(40m, await bob.GetBalanceAsync(destination.Id));

        var outgoing = await alice.GetFromJsonAsync<PagedResult<TransactionResponse>>(
            $"/api/accounts/{source.Id}/transactions?type=TransferOut", ApiClient.Json);
        var incoming = await bob.GetFromJsonAsync<PagedResult<TransactionResponse>>(
            $"/api/accounts/{destination.Id}/transactions?type=TransferIn", ApiClient.Json);

        Assert.Equal(transfer.Id, Assert.Single(outgoing!.Items).TransferId);
        Assert.Equal(transfer.Id, Assert.Single(incoming!.Items).TransferId);

        // Both parties can look the transfer up, a third user cannot.
        var stranger = await factory.CreateUserClientAsync();
        Assert.Equal(HttpStatusCode.OK, (await alice.GetAsync($"/api/transfers/{transfer.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await bob.GetAsync($"/api/transfers/{transfer.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/transfers/{transfer.Id}")).StatusCode);
    }

    [Fact]
    public async Task Transfer_with_insufficient_funds_returns_422_and_moves_nothing()
    {
        var client = await factory.CreateUserClientAsync();
        var source = await client.CreateAccountAsync(balance: 30m);
        var destination = await client.CreateAccountAsync();

        var response = await client.TransferAsync(source.Id, destination.Id, 30.01m);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(30m, await client.GetBalanceAsync(source.Id));
        Assert.Equal(0m, await client.GetBalanceAsync(destination.Id));
    }

    [Fact]
    public async Task Transfer_between_different_currencies_returns_422()
    {
        var client = await factory.CreateUserClientAsync();
        var lira = await client.CreateAccountAsync(balance: 100m, currency: Currency.TRY);
        var dollars = await client.CreateAccountAsync(currency: Currency.USD);

        var response = await client.TransferAsync(lira.Id, dollars.Id, 10m);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(100m, await client.GetBalanceAsync(lira.Id));
    }

    [Fact]
    public async Task Money_cannot_be_pulled_out_of_somebody_elses_account()
    {
        var alice = await factory.CreateUserClientAsync();
        var mallory = await factory.CreateUserClientAsync();
        var victim = await alice.CreateAccountAsync(balance: 100m);
        var loot = await mallory.CreateAccountAsync();

        var response = await mallory.TransferAsync(victim.Id, loot.Id, 100m);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(100m, await alice.GetBalanceAsync(victim.Id));
    }

    [Fact]
    public async Task Transfer_to_the_same_account_returns_400()
    {
        var client = await factory.CreateUserClientAsync();
        var account = await client.CreateAccountAsync(balance: 100m);

        var response = await client.TransferAsync(account.Id, account.Id, 10m);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Transfer_without_an_idempotency_key_returns_400()
    {
        var client = await factory.CreateUserClientAsync();
        var source = await client.CreateAccountAsync(balance: 100m);
        var destination = await client.CreateAccountAsync();

        var response = await client.TransferAsync(source.Id, destination.Id, 10m, omitIdempotencyKey: true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(100m, await client.GetBalanceAsync(source.Id));
    }

    [Fact]
    public async Task Repeating_a_transfer_with_the_same_key_does_not_move_the_money_twice()
    {
        var client = await factory.CreateUserClientAsync();
        var source = await client.CreateAccountAsync(balance: 100m);
        var destination = await client.CreateAccountAsync();
        var key = Guid.NewGuid().ToString();

        var first = await client.TransferAsync(source.Id, destination.Id, 25m, key);
        var retry = await client.TransferAsync(source.Id, destination.Id, 25m, key);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.True(retry.Headers.Contains(TransfersController.ReplayedHeader));
        Assert.Equal((await first.ReadAsync<TransferResponse>()).Id, (await retry.ReadAsync<TransferResponse>()).Id);
        Assert.Equal(75m, await client.GetBalanceAsync(source.Id));
        Assert.Equal(25m, await client.GetBalanceAsync(destination.Id));
    }

    [Fact]
    public async Task Reusing_a_key_for_a_different_transfer_returns_409()
    {
        var client = await factory.CreateUserClientAsync();
        var source = await client.CreateAccountAsync(balance: 100m);
        var destination = await client.CreateAccountAsync();
        var key = Guid.NewGuid().ToString();

        await client.TransferAsync(source.Id, destination.Id, 25m, key);
        var different = await client.TransferAsync(source.Id, destination.Id, 60m, key);

        Assert.Equal(HttpStatusCode.Conflict, different.StatusCode);
        Assert.Equal(75m, await client.GetBalanceAsync(source.Id));
    }

    [Fact]
    public async Task Concurrent_requests_with_the_same_key_execute_the_transfer_exactly_once()
    {
        var client = await factory.CreateUserClientAsync();
        var source = await client.CreateAccountAsync(balance: 100m);
        var destination = await client.CreateAccountAsync();
        var key = Guid.NewGuid().ToString();

        // A client that timed out and fired the same request several times in parallel.
        var responses = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => client.TransferAsync(source.Id, destination.Id, 10m, key)));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(90m, await client.GetBalanceAsync(source.Id));
        Assert.Equal(10m, await client.GetBalanceAsync(destination.Id));
    }

    [Fact]
    public async Task Concurrent_transfers_never_overdraw_the_account_or_create_money()
    {
        var client = await factory.CreateUserClientAsync();
        var source = await client.CreateAccountAsync(balance: 100m);
        var destination = await client.CreateAccountAsync();

        // 20 different transfers of 10 compete for a balance of 100: at most 10 can succeed.
        var responses = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(_ => client.TransferAsync(source.Id, destination.Id, 10m)));

        var succeeded = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var sourceBalance = await client.GetBalanceAsync(source.Id);
        var destinationBalance = await client.GetBalanceAsync(destination.Id);

        Assert.InRange(succeeded, 1, 10);
        Assert.All(responses, r => Assert.Contains(r.StatusCode,
            new[] { HttpStatusCode.Created, HttpStatusCode.Conflict, HttpStatusCode.UnprocessableEntity }));
        Assert.True(sourceBalance >= 0m);
        Assert.Equal(100m - succeeded * 10m, sourceBalance);
        Assert.Equal(succeeded * 10m, destinationBalance);
        Assert.Equal(100m, sourceBalance + destinationBalance); // money is neither created nor destroyed
    }
}
