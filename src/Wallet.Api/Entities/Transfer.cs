namespace Wallet.Api.Entities;

public class Transfer
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid FromAccountId { get; set; }

    public Guid ToAccountId { get; set; }

    public decimal Amount { get; set; }

    public Currency Currency { get; set; }

    public string? Description { get; set; }

    public Guid InitiatedByUserId { get; set; }

    /// <summary>
    /// Client-generated key that identifies one logical transfer. Unique per user,
    /// so a retried request can never move the money twice.
    /// </summary>
    public required string IdempotencyKey { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
