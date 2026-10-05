using Wallet.Api.Dtos;

namespace Wallet.Api.Services;

public interface ITransferService
{
    Task<TransferResponse> CreateAsync(Guid userId, CreateTransferRequest request, CancellationToken cancellationToken);

    Task<TransferResponse> GetByIdAsync(Guid userId, Guid transferId, CancellationToken cancellationToken);
}
