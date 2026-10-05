using Microsoft.EntityFrameworkCore;
using Wallet.Api.Data;
using Wallet.Api.Dtos;
using Wallet.Api.Entities;
using Wallet.Api.Exceptions;

namespace Wallet.Api.Services;

public sealed class TransferService(WalletDbContext db, TimeProvider timeProvider) : ITransferService
{
    public async Task<TransferResponse> CreateAsync(Guid userId, CreateTransferRequest request, CancellationToken cancellationToken)
    {
        var transfer = await ConcurrencyRetry.ExecuteAsync(
            db,
            () => ExecuteAsync(userId, request, cancellationToken),
            cancellationToken);

        return TransferResponse.From(transfer);
    }

    public async Task<TransferResponse> GetByIdAsync(Guid userId, Guid transferId, CancellationToken cancellationToken)
    {
        // Visible to the owner of either side of the transfer.
        var transfer = await db.Transfers
            .AsNoTracking()
            .Where(t => t.Id == transferId)
            .Where(t => db.Accounts.Any(a =>
                a.UserId == userId && (a.Id == t.FromAccountId || a.Id == t.ToAccountId)))
            .SingleOrDefaultAsync(cancellationToken);

        return transfer is null
            ? throw NotFoundException.For("Transfer", transferId)
            : TransferResponse.From(transfer);
    }

    private async Task<Transfer> ExecuteAsync(Guid userId, CreateTransferRequest request, CancellationToken cancellationToken)
    {
        var accounts = await db.Accounts
            .Where(a => a.Id == request.FromAccountId || a.Id == request.ToAccountId)
            .ToListAsync(cancellationToken);

        // The source must belong to the caller; the destination can be anybody's account.
        var source = accounts.SingleOrDefault(a => a.Id == request.FromAccountId && a.UserId == userId)
            ?? throw NotFoundException.For("Account", request.FromAccountId);

        var destination = accounts.SingleOrDefault(a => a.Id == request.ToAccountId)
            ?? throw NotFoundException.For("Account", request.ToAccountId);

        if (source.Id == destination.Id)
        {
            throw new BusinessRuleException("Source and destination accounts must be different.");
        }

        if (source.Currency != destination.Currency)
        {
            throw new BusinessRuleException(
                $"Cannot transfer between accounts in different currencies ({source.Currency} -> {destination.Currency}).");
        }

        // Throws InsufficientFundsException before anything is changed.
        source.Withdraw(request.Amount);
        destination.Deposit(request.Amount);

        var now = timeProvider.GetUtcNow();
        var description = request.Description?.Trim();

        var transfer = new Transfer
        {
            FromAccountId = source.Id,
            ToAccountId = destination.Id,
            Amount = request.Amount,
            Currency = source.Currency,
            Description = description,
            InitiatedByUserId = userId,
            CreatedAt = now
        };
        db.Transfers.Add(transfer);

        // Double entry: every transfer produces one ledger row on each side.
        db.Transactions.Add(new AccountTransaction
        {
            AccountId = source.Id,
            Type = TransactionType.TransferOut,
            Amount = request.Amount,
            BalanceAfter = source.Balance,
            TransferId = transfer.Id,
            Description = description,
            CreatedAt = now
        });
        db.Transactions.Add(new AccountTransaction
        {
            AccountId = destination.Id,
            Type = TransactionType.TransferIn,
            Amount = request.Amount,
            BalanceAfter = destination.Balance,
            TransferId = transfer.Id,
            Description = description,
            CreatedAt = now
        });

        // Atomicity: SaveChanges wraps the two UPDATEs and three INSERTs in a single database
        // transaction, so either both balances change or neither does.
        // Isolation: both UPDATEs carry "WHERE xmin = <version read above>". If another request
        // touched either account in the meantime, zero rows match, the transaction is rolled
        // back and DbUpdateConcurrencyException triggers a retry (see ConcurrencyRetry).
        await db.SaveChangesAsync(cancellationToken);

        return transfer;
    }
}
