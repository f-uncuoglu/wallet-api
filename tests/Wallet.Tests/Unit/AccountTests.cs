using Wallet.Api.Entities;
using Wallet.Api.Exceptions;

namespace Wallet.Tests.Unit;

public class AccountTests
{
    private static Account CreateAccount(decimal balance = 0m)
    {
        var account = new Account { Name = "Main", Currency = Currency.TRY, UserId = Guid.NewGuid() };
        if (balance > 0)
        {
            account.Deposit(balance);
        }

        return account;
    }

    [Fact]
    public void New_account_starts_with_zero_balance()
    {
        Assert.Equal(0m, CreateAccount().Balance);
    }

    [Fact]
    public void Deposit_increases_the_balance()
    {
        var account = CreateAccount(100m);

        account.Deposit(25.50m);

        Assert.Equal(125.50m, account.Balance);
    }

    [Fact]
    public void Withdraw_decreases_the_balance()
    {
        var account = CreateAccount(100m);

        account.Withdraw(40m);

        Assert.Equal(60m, account.Balance);
    }

    [Fact]
    public void Withdraw_of_the_whole_balance_is_allowed()
    {
        var account = CreateAccount(100m);

        account.Withdraw(100m);

        Assert.Equal(0m, account.Balance);
    }

    [Fact]
    public void Withdraw_more_than_the_balance_throws_and_changes_nothing()
    {
        var account = CreateAccount(100m);

        var exception = Assert.Throws<InsufficientFundsException>(() => account.Withdraw(100.01m));

        Assert.Equal(100m, account.Balance);
        Assert.Equal(100m, exception.Balance);
        Assert.Equal(100.01m, exception.Requested);
        Assert.Equal(422, exception.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Deposit_and_withdraw_reject_non_positive_amounts(int amount)
    {
        var account = CreateAccount(100m);

        Assert.Throws<ArgumentOutOfRangeException>(() => account.Deposit(amount));
        Assert.Throws<ArgumentOutOfRangeException>(() => account.Withdraw(amount));
        Assert.Equal(100m, account.Balance);
    }
}
