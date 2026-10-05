using Wallet.Api.Dtos;
using Wallet.Api.Validators;

namespace Wallet.Tests.Unit;

public class ValidatorTests
{
    private static readonly Guid AccountA = Guid.NewGuid();
    private static readonly Guid AccountB = Guid.NewGuid();

    [Fact]
    public void Transfer_with_valid_data_passes()
    {
        var result = new CreateTransferRequestValidator()
            .Validate(new CreateTransferRequest(AccountA, AccountB, 150.75m, "rent"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Transfer_to_the_same_account_is_rejected()
    {
        var result = new CreateTransferRequestValidator()
            .Validate(new CreateTransferRequest(AccountA, AccountA, 10m, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTransferRequest.ToAccountId));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("10.001")]
    [InlineData("1000000.01")]
    public void Transfer_with_invalid_amount_is_rejected(string amount)
    {
        var result = new CreateTransferRequestValidator()
            .Validate(new CreateTransferRequest(AccountA, AccountB, decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTransferRequest.Amount));
    }

    [Theory]
    [InlineData("0.01", true)]
    [InlineData("99.99", true)]
    [InlineData("1000000", true)]
    [InlineData("0.001", false)]
    public void Amounts_may_have_at_most_two_decimals(string amount, bool expected)
    {
        var value = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(expected, ValidationRules.HaveAtMostTwoDecimals(value));
    }

    [Theory]
    [InlineData("not-an-email", "Passw0rd!", "Ada Lovelace", nameof(RegisterRequest.Email))]
    [InlineData("ada@example.com", "short1", "Ada Lovelace", nameof(RegisterRequest.Password))]
    [InlineData("ada@example.com", "onlyletters", "Ada Lovelace", nameof(RegisterRequest.Password))]
    [InlineData("ada@example.com", "Passw0rd!", "", nameof(RegisterRequest.FullName))]
    public void Registration_with_invalid_data_is_rejected(string email, string password, string fullName, string invalidProperty)
    {
        var result = new RegisterRequestValidator().Validate(new RegisterRequest(email, password, fullName));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == invalidProperty);
    }

    [Fact]
    public void Registration_with_valid_data_passes()
    {
        var result = new RegisterRequestValidator()
            .Validate(new RegisterRequest("ada@example.com", "Passw0rd!", "Ada Lovelace"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void Transaction_query_rejects_invalid_paging(int page, int pageSize)
    {
        var result = new TransactionQueryValidator()
            .Validate(new TransactionQuery { Page = page, PageSize = pageSize });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Transaction_query_rejects_a_range_that_ends_before_it_starts()
    {
        var now = DateTimeOffset.UtcNow;

        var result = new TransactionQueryValidator()
            .Validate(new TransactionQuery { From = now, To = now.AddDays(-1) });

        Assert.False(result.IsValid);
    }
}
