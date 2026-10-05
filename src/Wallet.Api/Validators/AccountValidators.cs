using FluentValidation;
using Wallet.Api.Dtos;

namespace Wallet.Api.Validators;

public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(100);
        RuleFor(r => r.Currency).IsInEnum();
    }
}

public class DepositRequestValidator : AbstractValidator<DepositRequest>
{
    public DepositRequestValidator()
    {
        RuleFor(r => r.Amount).ValidMoneyAmount();
        RuleFor(r => r.Description).MaximumLength(200);
    }
}

public class WithdrawRequestValidator : AbstractValidator<WithdrawRequest>
{
    public WithdrawRequestValidator()
    {
        RuleFor(r => r.Amount).ValidMoneyAmount();
        RuleFor(r => r.Description).MaximumLength(200);
    }
}
