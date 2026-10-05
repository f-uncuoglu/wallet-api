using Wallet.Api.Entities;

namespace Wallet.Api.Dtos;

public record CreateTransferRequest(Guid FromAccountId, Guid ToAccountId, decimal Amount, string? Description);

public record TransferResponse(
    Guid Id,
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    Currency Currency,
    string? Description,
    DateTimeOffset CreatedAt)
{
    public static TransferResponse From(Transfer transfer) =>
        new(transfer.Id,
            transfer.FromAccountId,
            transfer.ToAccountId,
            transfer.Amount,
            transfer.Currency,
            transfer.Description,
            transfer.CreatedAt);
}
