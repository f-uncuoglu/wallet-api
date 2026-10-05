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
    /// <summary>Moves money between two accounts of the same currency.</summary>
    [HttpPost]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TransferResponse>> Create(CreateTransferRequest request, CancellationToken cancellationToken)
    {
        var transfer = await transferService.CreateAsync(User.GetUserId(), request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = transfer.Id }, transfer);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransferResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return await transferService.GetByIdAsync(User.GetUserId(), id, cancellationToken);
    }
}
