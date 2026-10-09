using EventFlow.Api.Auth;
using EventFlow.Api.Dtos.Auth;
using Microsoft.AspNetCore.Mvc;

namespace EventFlow.Api.Controllers;

[ApiController]
[Route("api/identity")]
public sealed class IdentityController : ControllerBase
{
    // Authorization is centralized so every controller interprets the demo
    // identity header and its failure cases consistently.
    private readonly EventFlowAuthorizationService _authorizationService;

    public IdentityController(
        EventFlowAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Resolves the seeded user represented by the X-Demo-Identity header.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(
        CancellationToken cancellationToken)
    {
        // The service reads X-Demo-Identity and resolves it against seeded users.
        var (decision, user) = await _authorizationService.RequireUserAsync(
            HttpContext,
            cancellationToken);

        if (!decision.Allowed || user is null)
        {
            // Missing or unknown identities become 401 Unauthorized.
            return this.ToActionResult(decision);
        }

        // Return a DTO instead of exposing the EF Core User entity directly.
        return Ok(new CurrentUserResponse
        {
            UserId = user.UserId,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Role = user.Role
        });
    }
}
