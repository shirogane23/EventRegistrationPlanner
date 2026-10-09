using EventFlow.Api.Auth;
using EventFlow.Api.Dtos.Registrations;
using EventFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventFlow.Api.Controllers;

[ApiController]
[Route("api/registrations")]
public sealed class RegistrationsController : ControllerBase
{
    private readonly EventFlowAuthorizationService _authorizationService;
    private readonly RegistrationService _registrationService;

    public RegistrationsController(
        EventFlowAuthorizationService authorizationService,
        RegistrationService registrationService)
    {
        _authorizationService = authorizationService;
        _registrationService = registrationService;
    }

    [HttpPost("/api/events/{eventId:guid}/registrations")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        Guid eventId,
        RegistrationCreateRequest request,
        CancellationToken cancellationToken)
    {
        var (decision, user) = await _authorizationService.RequireRoleAsync(
            HttpContext,
            DemoIdentityConstants.AttendeeRole,
            cancellationToken);

        if (!decision.Allowed || user is null)
        {
            return this.ToActionResult(decision);
        }

        var result = await _registrationService.CreateAsync(
            eventId,
            user.UserId,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RegistrationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrent(
        CancellationToken cancellationToken)
    {
        var (decision, user) = await _authorizationService.RequireUserAsync(
            HttpContext,
            cancellationToken);

        if (!decision.Allowed || user is null)
        {
            return this.ToActionResult(decision);
        }

        return Ok(await _registrationService.GetCurrentAsync(
            user.UserId,
            cancellationToken));
    }

    [HttpPost("{registrationId:guid}/cancel")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        Guid registrationId,
        CancellationToken cancellationToken)
    {
        var decision = await _authorizationService.RequireRegistrationOwnerAsync(
            HttpContext,
            registrationId,
            cancellationToken);

        if (!decision.Allowed)
        {
            return this.ToActionResult(decision);
        }

        var (_, user) = await _authorizationService.RequireUserAsync(
            HttpContext,
            cancellationToken);

        return ToActionResult(await _registrationService.CancelAsync(
            registrationId,
            user!.UserId,
            cancellationToken));
    }

    private IActionResult ToActionResult(
        EventServiceResult<RegistrationResponse> result)
    {
        if (result.Succeeded)
        {
            return result.Value is null
                ? NoContent()
                : Ok(result.Value);
        }

        return result.Error!.Code == EventServiceErrorCode.NotFound
            ? NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found",
                Detail = result.Error.Detail
            })
            : Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Registration conflict",
                Detail = result.Error.Detail
            });
    }
}
