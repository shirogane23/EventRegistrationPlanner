using EventFlow.Api.Auth;
using EventFlow.Api.Dtos.Auth;
using Microsoft.AspNetCore.Mvc;

namespace EventFlow.Api.Controllers;

[ApiController]
[Route("api/identity")]
public sealed class IdentityController : ControllerBase
{
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
        var (decision, user) = await _authorizationService.RequireUserAsync(
            HttpContext,
            cancellationToken);

        if (!decision.Allowed || user is null)
        {
            return this.ToActionResult(decision);
        }

        return Ok(new CurrentUserResponse
        {
            UserId = user.UserId,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Role = user.Role
        });
    }
}
