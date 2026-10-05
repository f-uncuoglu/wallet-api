using Wallet.Api.Entities;

namespace Wallet.Api.Dtos;

// The request deliberately has no Balance: a client can never set it (over-posting).
// Every account starts at zero and only changes through deposits, withdrawals and transfers.
public record CreateAccountRequest(string Name, Currency Currency);

public record AccountResponse(Guid Id, string Name, Currency Currency, decimal Balance, DateTimeOffset CreatedAt)
{
    public static AccountResponse From(Account account) =>
        new(account.Id, account.Name, account.Currency, account.Balance, account.CreatedAt);
}

public record DepositRequest(decimal Amount, string? Description);

public record WithdrawRequest(decimal Amount, string? Description);

public record TransactionResponse(
    Guid Id,
    Guid AccountId,
    TransactionType Type,
    decimal Amount,
    decimal BalanceAfter,
    Guid? TransferId,
    string? Description,
    DateTimeOffset CreatedAt)
{
    public static TransactionResponse From(AccountTransaction transaction) =>
        new(transaction.Id,
            transaction.AccountId,
            transaction.Type,
            transaction.Amount,
            transaction.BalanceAfter,
            transaction.TransferId,
            transaction.Description,
            transaction.CreatedAt);
}
