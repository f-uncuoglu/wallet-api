using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Dtos;
using Wallet.Api.Infrastructure;
using Wallet.Api.Services;

namespace Wallet.Api.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize]
public class AccountsController(IAccountService accountService) : ControllerBase
{
    /// <summary>Opens a new account for the authenticated user. The balance starts at zero.</summary>
    [HttpPost]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AccountResponse>> Create(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var account = await accountService.CreateAsync(User.GetUserId(), request, cancellationToken);

        // 201 + Location header pointing at the new resource.
        return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
    }

    /// <summary>Lists the accounts of the authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AccountResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AccountResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await accountService.GetAllAsync(User.GetUserId(), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return await accountService.GetByIdAsync(User.GetUserId(), id, cancellationToken);
    }
}
