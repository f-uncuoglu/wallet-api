using Wallet.Api.Dtos;

namespace Wallet.Api.Services;

public interface IAccountService
{
    Task<AccountResponse> CreateAsync(Guid userId, CreateAccountRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<AccountResponse>> GetAllAsync(Guid userId, CancellationToken cancellationToken);

    Task<AccountResponse> GetByIdAsync(Guid userId, Guid accountId, CancellationToken cancellationToken);

    Task<TransactionResponse> DepositAsync(Guid userId, Guid accountId, DepositRequest request, CancellationToken cancellationToken);

    Task<TransactionResponse> WithdrawAsync(Guid userId, Guid accountId, WithdrawRequest request, CancellationToken cancellationToken);
}
