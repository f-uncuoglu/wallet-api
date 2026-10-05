using FluentValidation;
using Wallet.Api.Dtos;

namespace Wallet.Api.Validators;

public class TransactionQueryValidator : AbstractValidator<TransactionQuery>
{
    public TransactionQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, PageQuery.MaxPageSize);
        RuleFor(q => q.Type).IsInEnum();

        RuleFor(q => q.To)
            .Must((query, to) => to > query.From)
            .WithMessage("'To' must be later than 'From'.")
            .When(q => q.From.HasValue && q.To.HasValue);
    }
}

public class PageQueryValidator : AbstractValidator<PageQuery>
{
    public PageQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, PageQuery.MaxPageSize);
    }
}
