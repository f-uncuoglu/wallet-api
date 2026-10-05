using System.Net;
using System.Net.Http.Json;
using Wallet.Api.Dtos;

namespace Wallet.Tests.Integration;

[Collection(ApiCollection.Name)]
public class AdminTests(WalletApiFactory factory)
{
    [Fact]
    public async Task Regular_user_is_forbidden_from_admin_endpoints()
    {
        var client = await factory.CreateUserClientAsync();

        var response = await client.GetAsync("/api/admin/accounts");

        // Authenticated but not allowed: 403, not 401.
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_sees_the_accounts_of_all_users()
    {
        var alice = await factory.CreateUserClientAsync();
        var bob = await factory.CreateUserClientAsync();
        var aliceAccount = await alice.CreateAccountAsync(balance: 10m);
        var bobAccount = await bob.CreateAccountAsync(balance: 20m);
        var admin = await factory.CreateAdminClientAsync();

        var page = await admin.GetFromJsonAsync<PagedResult<AdminAccountResponse>>(
            "/api/admin/accounts?page=1&pageSize=100", ApiClient.Json);

        Assert.Contains(page!.Items, a => a.Id == aliceAccount.Id && a.Balance == 10m);
        Assert.Contains(page.Items, a => a.Id == bobAccount.Id && a.Balance == 20m);
        Assert.All(page.Items, a => Assert.False(string.IsNullOrEmpty(a.OwnerEmail)));
    }
}
