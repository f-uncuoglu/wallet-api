using FluentValidation;
using Wallet.Api.Dtos;

namespace Wallet.Api.Validators;

public class CreateTransferRequestValidator : AbstractValidator<CreateTransferRequest>
{
    public CreateTransferRequestValidator()
    {
        RuleFor(r => r.FromAccountId).NotEmpty();
        RuleFor(r => r.ToAccountId).NotEmpty();

        RuleFor(r => r.ToAccountId)
            .NotEqual(r => r.FromAccountId)
            .WithMessage("Source and destination accounts must be different.");

        RuleFor(r => r.Amount).ValidMoneyAmount();
        RuleFor(r => r.Description).MaximumLength(200);
    }
}
