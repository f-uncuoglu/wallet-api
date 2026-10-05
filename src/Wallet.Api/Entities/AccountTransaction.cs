namespace Wallet.Api.Entities;

public enum TransactionType
{
    Deposit,
    Withdrawal,
    TransferIn,
    TransferOut
}

/// <summary>
/// One immutable line in an account's ledger. Rows are only ever inserted,
/// never updated, so the history of a balance can always be reconstructed.
/// </summary>
public class AccountTransaction
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid AccountId { get; set; }

    public Account? Account { get; set; }

    public TransactionType Type { get; set; }

    /// <summary>Always positive; the direction is given by <see cref="Type"/>.</summary>
    public decimal Amount { get; set; }

    /// <summary>Balance of the account right after this transaction was applied.</summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>Set for TransferIn / TransferOut entries, null otherwise.</summary>
    public Guid? TransferId { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
