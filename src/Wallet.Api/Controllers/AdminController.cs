using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Dtos;
using Wallet.Api.Entities;
using Wallet.Api.Services;

namespace Wallet.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminController(IAdminService adminService) : ControllerBase
{
    /// <summary>All accounts in the system with their owners. Admins only (403 for regular users).</summary>
    [HttpGet("accounts")]
    [ProducesResponseType<PagedResult<AdminAccountResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AdminAccountResponse>>> GetAccounts(
        [FromQuery] PageQuery query,
        CancellationToken cancellationToken)
    {
        return await adminService.GetAccountsAsync(query, cancellationToken);
    }
}
