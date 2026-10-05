using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Dtos;
using Wallet.Api.Infrastructure;
using Wallet.Api.Services;

namespace Wallet.Api.Controllers;

[ApiController]
[Route("api/accounts/{accountId:guid}/transactions")]
[Authorize]
public class TransactionsController(ITransactionQueryService transactionQueryService) : ControllerBase
{
    /// <summary>
    /// Transaction history of an account, newest first. Supports paging (page, pageSize)
    /// and filtering by type and by a [from, to) time range.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<TransactionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<TransactionResponse>>> GetForAccount(
        Guid accountId,
        [FromQuery] TransactionQuery query,
        CancellationToken cancellationToken)
    {
        return await transactionQueryService.GetForAccountAsync(User.GetUserId(), accountId, query, cancellationToken);
    }
}
