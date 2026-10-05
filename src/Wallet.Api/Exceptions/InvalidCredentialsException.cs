namespace Wallet.Api.Exceptions;

public sealed class InvalidCredentialsException() : DomainException("Email or password is incorrect.")
{
    public override int StatusCode => StatusCodes.Status401Unauthorized;

    public override string Title => "Invalid credentials";
}
