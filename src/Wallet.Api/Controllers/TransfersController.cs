using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Dtos;
using Wallet.Api.Infrastructure;
using Wallet.Api.Services;

namespace Wallet.Api.Controllers;

[ApiController]
[Route("api/transfers")]
[Authorize]
public class TransfersController(ITransferService transferService) : ControllerBase
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";
    public const string ReplayedHeader = "Idempotent-Replayed";
    private const int MaxIdempotencyKeyLength = 100;

    /// <summary>
    /// Moves money between two accounts of the same currency. The request must carry an
    /// Idempotency-Key header; repeating a request with the same key returns the original
    /// transfer (200) instead of executing it again.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TransferResponse>> Create(
        CreateTransferRequest request,
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        idempotencyKey = idempotencyKey?.Trim();

        if (string.IsNullOrEmpty(idempotencyKey) || idempotencyKey.Length > MaxIdempotencyKeyLength)
        {
            ModelState.AddModelError(
                IdempotencyKeyHeader,
                $"The {IdempotencyKeyHeader} header is required and must be at most {MaxIdempotencyKeyLength} characters.");
            return ValidationProblem(ModelState);
        }

        var result = await transferService.CreateAsync(User.GetUserId(), request, idempotencyKey, cancellationToken);

        if (result.Replayed)
        {
            Response.Headers[ReplayedHeader] = "true";
            return Ok(result.Transfer);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Transfer.Id }, result.Transfer);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransferResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return await transferService.GetByIdAsync(User.GetUserId(), id, cancellationToken);
    }
}
