namespace Wallet.Api.Exceptions;

/// <summary>The request is well-formed but violates a business rule (HTTP 422).</summary>
public class BusinessRuleException(string message) : DomainException(message)
{
    public override int StatusCode => StatusCodes.Status422UnprocessableEntity;

    public override string Title => "Business rule violated";
}
