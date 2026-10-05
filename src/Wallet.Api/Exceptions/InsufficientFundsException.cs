namespace Wallet.Api.Exceptions;

public sealed class InsufficientFundsException(Guid accountId, decimal balance, decimal requested)
    : BusinessRuleException($"Account '{accountId}' has insufficient funds: balance is {balance:0.00}, requested {requested:0.00}.")
{
    public override string Title => "Insufficient funds";

    public Guid AccountId { get; } = accountId;

    public decimal Balance { get; } = balance;

    public decimal Requested { get; } = requested;
}
