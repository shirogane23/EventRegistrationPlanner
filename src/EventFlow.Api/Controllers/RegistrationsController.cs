using EventFlow.Api.Auth;
using EventFlow.Api.Dtos.Registrations;
using EventFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventFlow.Api.Controllers;

[ApiController]
[Route("api/registrations")]
public sealed class RegistrationsController : ControllerBase
{
    // Authorization is kept separate from registration business rules.
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
        // Registration creation is restricted to the Attendee role.
        var (decision, user) = await _authorizationService.RequireRoleAsync(
            HttpContext,
            DemoIdentityConstants.AttendeeRole,
            cancellationToken);

        if (!decision.Allowed || user is null)
        {
            return this.ToActionResult(decision);
        }

        // The route supplies the event and the resolved identity supplies the
        // attendee; the request body cannot overpost server-owned fields.
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
        // Any authenticated user can read only their own current registrations.
        var (decision, user) = await _authorizationService.RequireUserAsync(
            HttpContext,
            cancellationToken);

        if (!decision.Allowed || user is null)
        {
            return this.ToActionResult(decision);
        }

        // Cancelled historical rows are intentionally excluded by the service.
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
        // This check prevents one attendee from cancelling another attendee's
        // registration before the service changes its status.
        var decision = await _authorizationService.RequireRegistrationOwnerAsync(
            HttpContext,
            registrationId,
            cancellationToken);

        if (!decision.Allowed)
        {
            return this.ToActionResult(decision);
        }

        // Cancellation is a status transition; the database row is preserved.
        var (_, user) = await _authorizationService.RequireUserAsync(
            HttpContext,
            cancellationToken);

        return ToActionResult(await _registrationService.CancelAsync(
            registrationId,
            user!.UserId,
            cancellationToken));
    }

    [HttpGet("/api/events/{eventId:guid}/registrations")]
    [ProducesResponseType(typeof(IReadOnlyList<OrganizerRegistrationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForOwnedEvent(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        // Only the event owner can see the complete guest list, including
        // cancelled rows retained for history.
        var decision = await _authorizationService.RequireEventOwnerAsync(
            HttpContext,
            eventId,
            cancellationToken);

        if (!decision.Allowed)
        {
            return this.ToActionResult(decision);
        }

        // The service returns attendee details and status-based capacity data.
        var (_, user) = await _authorizationService.RequireUserAsync(
            HttpContext,
            cancellationToken);

        var result = await _registrationService.GetForOwnedEventAsync(
            eventId,
            user!.UserId,
            cancellationToken);

        return ToOrganizerListActionResult(result);
    }

    [HttpPost("{registrationId:guid}/approve")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(
        Guid registrationId,
        CancellationToken cancellationToken)
    {
        // Approval is an organizer operation on a registration belonging to
        // one of that organizer's events.
        var decision = await _authorizationService.RequireEventRegistrationOwnerAsync(
            HttpContext,
            registrationId,
            cancellationToken);

        if (!decision.Allowed)
        {
            return this.ToActionResult(decision);
        }

        // The service verifies Pending status and creates the confirmation
        // reference when the transition succeeds.
        var (_, user) = await _authorizationService.RequireUserAsync(
            HttpContext,
            cancellationToken);

        return ToActionResult(await _registrationService.ApproveAsync(
            registrationId,
            user!.UserId,
            cancellationToken));
    }

    [HttpPost("{registrationId:guid}/reject")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(
        Guid registrationId,
        RegistrationActionRequest request,
        CancellationToken cancellationToken)
    {
        // Rejection requires a validated reason and becomes a Cancelled record
        // rather than deleting registration history.
        return await ApplyOrganizerActionAsync(
            registrationId,
            (userId, token) => _registrationService.RejectAsync(
                registrationId,
                userId,
                request.Reason,
                token),
            cancellationToken);
    }

    [HttpPost("{registrationId:guid}/organizer-cancel")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelByOrganizer(
        Guid registrationId,
        RegistrationActionRequest request,
        CancellationToken cancellationToken)
    {
        // Organizer cancellation also requires a reason and releases capacity
        // by changing the status to Cancelled.
        return await ApplyOrganizerActionAsync(
            registrationId,
            (userId, token) => _registrationService.CancelByOrganizerAsync(
                registrationId,
                userId,
                request.Reason,
                token),
            cancellationToken);
    }

    private async Task<IActionResult> ApplyOrganizerActionAsync(
        Guid registrationId,
        Func<Guid, CancellationToken, Task<EventServiceResult<RegistrationResponse>>> action,
        CancellationToken cancellationToken)
    {
        // Shared helper for organizer rejection and organizer cancellation.
        var decision = await _authorizationService.RequireEventRegistrationOwnerAsync(
            HttpContext,
            registrationId,
            cancellationToken);

        if (!decision.Allowed)
        {
            return this.ToActionResult(decision);
        }

        // The callback selects the exact business operation after authorization.
        var (_, user) = await _authorizationService.RequireUserAsync(
            HttpContext,
            cancellationToken);

        return ToActionResult(await action(user!.UserId, cancellationToken));
    }

    private IActionResult ToOrganizerListActionResult(
        EventServiceResult<IReadOnlyList<OrganizerRegistrationResponse>> result)
    {
        if (result.Succeeded)
        {
            // The list endpoint returns the complete organizer guest list.
            return Ok(result.Value);
        }

        // An event outside the organizer's ownership scope is not exposed.
        return NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Resource not found",
            Detail = result.Error!.Detail
        });
    }

    private IActionResult ToActionResult(
        EventServiceResult<RegistrationResponse> result)
    {
        if (result.Succeeded)
        {
            // Registration creation returns the created resource; other
            // controller actions reuse this mapper for their result shape.
            return result.Value is null
                ? NoContent()
                : StatusCode(StatusCodes.Status201Created, result.Value);
        }

        // NotFound and Conflict are deliberately represented as Problem Details
        // so Swagger and clients receive a predictable error contract.
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
