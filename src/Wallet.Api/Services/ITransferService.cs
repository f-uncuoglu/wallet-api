using Wallet.Api.Dtos;

namespace Wallet.Api.Services;

public interface ITransferService
{
    Task<TransferResult> CreateAsync(Guid userId, CreateTransferRequest request, string idempotencyKey, CancellationToken cancellationToken);

    Task<TransferResponse> GetByIdAsync(Guid userId, Guid transferId, CancellationToken cancellationToken);
}
