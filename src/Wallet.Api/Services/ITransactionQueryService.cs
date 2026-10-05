using Wallet.Api.Dtos;

namespace Wallet.Api.Services;

public interface ITransactionQueryService
{
    Task<PagedResult<TransactionResponse>> GetForAccountAsync(
        Guid userId,
        Guid accountId,
        TransactionQuery query,
        CancellationToken cancellationToken);
}
