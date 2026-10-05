using Microsoft.EntityFrameworkCore;
using Wallet.Api.Data;
using Wallet.Api.Dtos;

namespace Wallet.Api.Services;

public sealed class AdminService(WalletDbContext db) : IAdminService
{
    public async Task<PagedResult<AdminAccountResponse>> GetAccountsAsync(PageQuery query, CancellationToken cancellationToken)
    {
        var totalCount = await db.Accounts.CountAsync(cancellationToken);

        // The projection references a.User, so EF Core generates the JOIN itself;
        // no Include and no N+1 queries.
        var items = await db.Accounts
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new AdminAccountResponse(
                a.Id,
                a.UserId,
                a.User!.Email,
                a.Name,
                a.Currency,
                a.Balance,
                a.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminAccountResponse>(items, query.Page, query.PageSize, totalCount);
    }
}
