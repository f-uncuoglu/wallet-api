using Microsoft.EntityFrameworkCore;
using Wallet.Api.Exceptions;

namespace Wallet.Api.Data;

/// <summary>
/// Optimistic concurrency: nothing is locked while a request works on an account. Instead the
/// UPDATE only succeeds if the row still has the version that was read. When it does not,
/// EF Core throws <see cref="DbUpdateConcurrencyException"/> and the operation is repeated
/// from the start with fresh data.
/// </summary>
public static class ConcurrencyRetry
{
    public const int MaxAttempts = 5;

    public static async Task<T> ExecuteAsync<T>(
        WalletDbContext db,
        Func<Task<T>> operation,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxAttempts)
            {
                // Forget the stale entities, otherwise the retry would read them from the
                // change tracker instead of the database.
                db.ChangeTracker.Clear();

                // Small randomised back-off so competing requests do not collide again immediately.
                await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(5, 25) * attempt), cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException(
                    "The account is being modified by other requests. Please try again.");
            }
        }
    }
}
