using FluentValidation;
using Wallet.Api.Dtos;

namespace Wallet.Api.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(r => r.Email).NotEmpty().EmailAddress().MaximumLength(256);

        RuleFor(r => r.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Must(password => password is not null && password.Any(char.IsDigit) && password.Any(char.IsLetter))
            .WithMessage("Password must contain at least one letter and one digit.");

        RuleFor(r => r.FullName).NotEmpty().MaximumLength(100);
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.Email).NotEmpty();
        RuleFor(r => r.Password).NotEmpty();
    }
}
