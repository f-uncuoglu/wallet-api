using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Wallet.Api.Exceptions;
using Wallet.Api.Infrastructure;

namespace Wallet.Tests.Unit;

public class GlobalExceptionHandlerTests
{
    private readonly Mock<IProblemDetailsService> _problemDetailsService = new();
    private ProblemDetailsContext? _written;

    private GlobalExceptionHandler CreateHandler()
    {
        _problemDetailsService
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .Callback<ProblemDetailsContext>(context => _written = context)
            .ReturnsAsync(true);

        return new GlobalExceptionHandler(_problemDetailsService.Object, NullLogger<GlobalExceptionHandler>.Instance);
    }

    [Fact]
    public async Task Domain_exception_is_mapped_to_its_own_status_code()
    {
        var httpContext = new DefaultHttpContext();
        var exception = new InsufficientFundsException(Guid.NewGuid(), balance: 10m, requested: 50m);

        var handled = await CreateHandler().TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, httpContext.Response.StatusCode);
        Assert.NotNull(_written);
        Assert.Equal("Insufficient funds", _written.ProblemDetails.Title);
        Assert.Equal(exception.Message, _written.ProblemDetails.Detail);
    }

    [Fact]
    public async Task Not_found_exception_is_mapped_to_404()
    {
        var httpContext = new DefaultHttpContext();

        await CreateHandler().TryHandleAsync(httpContext, NotFoundException.For("Account", Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task Unexpected_exception_becomes_a_500_without_leaking_details()
    {
        var httpContext = new DefaultHttpContext();
        var exception = new InvalidOperationException("connection string: Host=prod;Password=secret");

        await CreateHandler().TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        Assert.NotNull(_written);
        Assert.Null(_written.ProblemDetails.Detail);
        Assert.DoesNotContain("secret", _written.ProblemDetails.Title);
    }
}
