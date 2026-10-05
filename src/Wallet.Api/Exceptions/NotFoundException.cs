namespace Wallet.Api.Exceptions;

public sealed class NotFoundException(string message) : DomainException(message)
{
    public override int StatusCode => StatusCodes.Status404NotFound;

    public override string Title => "Resource not found";

    public static NotFoundException For(string resource, Guid id) => new($"{resource} '{id}' was not found.");
}
