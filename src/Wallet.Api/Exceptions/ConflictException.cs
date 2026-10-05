namespace Wallet.Api.Exceptions;

/// <summary>The request is valid but clashes with the current state of the resource.</summary>
public sealed class ConflictException(string message) : DomainException(message)
{
    public override int StatusCode => StatusCodes.Status409Conflict;

    public override string Title => "Conflict";
}
