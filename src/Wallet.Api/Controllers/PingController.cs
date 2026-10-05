using Microsoft.AspNetCore.Mvc;

namespace Wallet.Api.Controllers;

[ApiController]
[Route("api/ping")]
public class PingController : ControllerBase
{
    /// <summary>Returns the service name and the server time. Useful as a smoke test.</summary>
    [HttpGet]
    public IActionResult Get() => Ok(new { service = "wallet-api", utcNow = DateTimeOffset.UtcNow });
}
