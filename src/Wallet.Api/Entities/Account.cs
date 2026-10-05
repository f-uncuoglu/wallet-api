using Wallet.Api.Exceptions;

namespace Wallet.Api.Entities;

public class Account
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public required string Name { get; set; }

    public Currency Currency { get; set; }

    /// <summary>
    /// Only changes through <see cref="Deposit"/> and <see cref="Withdraw"/>, so the
    /// "never negative" rule lives in one place instead of in every caller.
    /// </summary>
    public decimal Balance { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Optimistic concurrency token, mapped to PostgreSQL's xmin system column.
    /// It changes on every UPDATE of the row, so a stale write is detected by EF Core.
    /// </summary>
    public uint Version { get; private set; }

    public void Deposit(decimal amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);

        Balance += amount;
    }

    public void Withdraw(decimal amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);

        if (amount > Balance)
        {
            throw new InsufficientFundsException(Id, Balance, amount);
        }

        Balance -= amount;
    }
}
