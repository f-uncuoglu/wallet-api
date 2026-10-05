using FluentValidation;

namespace Wallet.Api.Validators;

/// <summary>Rules shared by several validators, so "what is a valid amount" is defined once.</summary>
public static class ValidationRules
{
    public const decimal MaxAmount = 1_000_000m;

    public static IRuleBuilderOptions<T, decimal> ValidMoneyAmount<T>(this IRuleBuilder<T, decimal> rule) =>
        rule
            .GreaterThan(0m).WithMessage("Amount must be greater than zero.")
            .LessThanOrEqualTo(MaxAmount).WithMessage($"Amount must not exceed {MaxAmount:0}.")
            .Must(HaveAtMostTwoDecimals).WithMessage("Amount must not have more than two decimal places.");

    public static bool HaveAtMostTwoDecimals(decimal amount) => decimal.Round(amount, 2) == amount;
}
