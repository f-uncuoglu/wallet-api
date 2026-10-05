namespace Wallet.Api.Exceptions;

/// <summary>
/// Base class for every "expected" failure: a broken business rule, a missing resource, ...
/// Each subclass knows which HTTP status it maps to, so the services can simply throw and
/// the <see cref="Infrastructure.GlobalExceptionHandler"/> turns it into a ProblemDetails response.
/// </summary>
public abstract class DomainException(string message) : Exception(message)
{
    public abstract int StatusCode { get; }

    public abstract string Title { get; }
}
