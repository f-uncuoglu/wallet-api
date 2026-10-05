using Microsoft.EntityFrameworkCore;
using Wallet.Api.Data;
using Wallet.Api.Dtos;
using Wallet.Api.Exceptions;

namespace Wallet.Api.Services;

public sealed class TransactionQueryService(WalletDbContext db) : ITransactionQueryService
{
    public async Task<PagedResult<TransactionResponse>> GetForAccountAsync(
        Guid userId,
        Guid accountId,
        TransactionQuery query,
        CancellationToken cancellationToken)
    {
        var ownsAccount = await db.Accounts.AnyAsync(a => a.Id == accountId && a.UserId == userId, cancellationToken);
        if (!ownsAccount)
        {
            throw NotFoundException.For("Account", accountId);
        }

        // Nothing is sent to the database yet: each Where only extends the expression tree.
        var transactions = db.Transactions
            .AsNoTracking()
            .Where(t => t.AccountId == accountId);

        if (query.Type is { } type)
        {
            transactions = transactions.Where(t => t.Type == type);
        }

        if (query.From is { } from)
        {
            // timestamptz parameters must be UTC for Npgsql.
            var fromUtc = from.ToUniversalTime();
            transactions = transactions.Where(t => t.CreatedAt >= fromUtc);
        }

        if (query.To is { } to)
        {
            var toUtc = to.ToUniversalTime();
            transactions = transactions.Where(t => t.CreatedAt < toUtc);
        }

        // Two round trips: COUNT(*) for the total, then one page via ORDER BY ... LIMIT/OFFSET.
        var totalCount = await transactions.CountAsync(cancellationToken);

        var items = await transactions
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new TransactionResponse(
                t.Id,
                t.AccountId,
                t.Type,
                t.Amount,
                t.BalanceAfter,
                t.TransferId,
                t.Description,
                t.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<TransactionResponse>(items, query.Page, query.PageSize, totalCount);
    }
}
