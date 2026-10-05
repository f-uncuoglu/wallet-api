using Microsoft.EntityFrameworkCore;
using Wallet.Api.Data;
using Wallet.Api.Dtos;
using Wallet.Api.Entities;
using Wallet.Api.Exceptions;

namespace Wallet.Api.Services;

public sealed class AccountService(WalletDbContext db, TimeProvider timeProvider) : IAccountService
{
    public async Task<AccountResponse> CreateAsync(Guid userId, CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var account = new Account
        {
            UserId = userId,
            Name = request.Name.Trim(),
            Currency = request.Currency,
            CreatedAt = timeProvider.GetUtcNow()
        };

        db.Accounts.Add(account);
        await db.SaveChangesAsync(cancellationToken);

        return AccountResponse.From(account);
    }

    public async Task<IReadOnlyList<AccountResponse>> GetAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Read-only query: no change tracking, and only the needed columns are selected.
        return await db.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.CreatedAt)
            .Select(a => new AccountResponse(a.Id, a.Name, a.Currency, a.Balance, a.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<AccountResponse> GetByIdAsync(Guid userId, Guid accountId, CancellationToken cancellationToken)
    {
        var account = await db.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == accountId && a.UserId == userId, cancellationToken);

        // An account that belongs to somebody else is reported as "not found" (not 403),
        // so the API does not reveal which account ids exist.
        return account is null
            ? throw NotFoundException.For("Account", accountId)
            : AccountResponse.From(account);
    }
}
