using Wallet.Api.Entities;

namespace Wallet.Api.Dtos;

public record AdminAccountResponse(
    Guid Id,
    Guid OwnerId,
    string OwnerEmail,
    string Name,
    Currency Currency,
    decimal Balance,
    DateTimeOffset CreatedAt);
