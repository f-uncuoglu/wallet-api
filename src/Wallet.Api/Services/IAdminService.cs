using Wallet.Api.Dtos;

namespace Wallet.Api.Services;

public interface IAdminService
{
    Task<PagedResult<AdminAccountResponse>> GetAccountsAsync(PageQuery query, CancellationToken cancellationToken);
}
