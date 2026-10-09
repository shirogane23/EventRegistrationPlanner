using EventFlow.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    // The controller receives the database context through dependency injection.
    // This keeps the health check connected to the same database configuration as
    // the rest of the API.
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
        // CanConnectAsync checks the database without loading application data.
        var databaseAvailable =
            await _dbContext.Database.CanConnectAsync(cancellationToken);

        if (!databaseAvailable)
        {
            // 503 tells monitoring clients that the API is running but its
            // required database dependency is unavailable.
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "Unavailable",
                database = "Disconnected"
            });
        }

        // A successful response confirms both API availability and connectivity.
        return Ok(new
        {
            status = "Healthy",
            database = "Connected"
        });
    }
}
