using EventFlow.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    private readonly EventFlowDbContext _dbContext;

    public HealthController(EventFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Verifies that the API is running and can connect to EventFlowDb.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var databaseAvailable =
            await _dbContext.Database.CanConnectAsync(cancellationToken);

        if (!databaseAvailable)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "Unavailable",
                database = "Disconnected"
            });
        }

        return Ok(new
        {
            status = "Healthy",
            database = "Connected"
        });
    }
}
