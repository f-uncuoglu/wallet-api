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
