using Wallet.Api.Entities;

namespace Wallet.Api.Dtos;

/// <summary>Query string of GET /api/accounts/{id}/transactions.</summary>
public class TransactionQuery : PageQuery
{
    /// <summary>Only return transactions of this type.</summary>
    public TransactionType? Type { get; set; }

    /// <summary>Only return transactions created at or after this instant.</summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>Only return transactions created before this instant.</summary>
    public DateTimeOffset? To { get; set; }
}
