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

    public DateTimeOffset CreatedAt { get; set; }
}
