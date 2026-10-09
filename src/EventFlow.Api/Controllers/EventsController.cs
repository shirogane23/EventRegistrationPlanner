using EventFlow.Api.Auth;
using EventFlow.Api.Dtos.Events;
using EventFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventFlow.Api.Controllers;

[ApiController]
[Route("api/events")]
public sealed class EventsController : ControllerBase
{
    // Controllers coordinate HTTP concerns. Business rules remain in EventService.
    private readonly EventFlowAuthorizationService _authorizationService;
    private readonly EventService _eventService;

    public EventsController(
        EventFlowAuthorizationService authorizationService,
        EventService eventService)
    {
        _authorizationService = authorizationService;
        _eventService = eventService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EventSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? title,
        CancellationToken cancellationToken) =>
        // Public search does not require an identity header.
        Ok(await _eventService.SearchPublicAsync(title, cancellationToken));

    [HttpGet("{eventId:guid}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        // The service applies the direct-lookup visibility and lifecycle rules.
        var result = await _eventService.GetPublicAsync(
            eventId,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        EventCreateRequest request,
        CancellationToken cancellationToken)
    {
        // Only an authenticated organizer may create an event.
        var (decision, user) = await _authorizationService.RequireRoleAsync(
            HttpContext,
            DemoIdentityConstants.OrganizerRole,
            cancellationToken);

        if (!decision.Allowed || user is null)
        {
            // This returns 401 for identity failures and 403 for role failures.
            return this.ToActionResult(decision);
        }

        // The resolved user ID, not a client field, becomes the event owner.
        var result = await _eventService.CreateAsync(
            request,
            user.UserId,
            cancellationToken);

        if (!result.Succeeded)
        {
            // Service conflicts include invalid venues, capacity, and schedules.
            return ToActionResult(result);
        }

        // CreatedAtAction returns 201 and a link to retrieve the new event.
        return CreatedAtAction(
            nameof(Get),
            new { eventId = result.Value!.EventId },
            result.Value);
    }

    [HttpGet("owned/{eventId:guid}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOwned(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        // Role authorization confirms the caller is an organizer.
        var (decision, user) = await _authorizationService.RequireRoleAsync(
            HttpContext,
            DemoIdentityConstants.OrganizerRole,
            cancellationToken);

        if (!decision.Allowed || user is null)
        {
            return this.ToActionResult(decision);
        }

        // EventService also enforces that this organizer owns the event.
        return ToActionResult(await _eventService.GetOwnedAsync(
            eventId,
            user.UserId,
            cancellationToken));
    }

    [HttpPut("{eventId:guid}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid eventId,
        EventUpdateRequest request,
        CancellationToken cancellationToken)
    {
        // Ownership is checked before any update is sent to the service layer.
        var decision = await _authorizationService.RequireEventOwnerAsync(
            HttpContext,
            eventId,
            cancellationToken);

        if (!decision.Allowed)
        {
            return this.ToActionResult(decision);
        }

        // The owner ID is used again by the service to scope the database query.
        var (_, user) = await _authorizationService.RequireUserAsync(
            HttpContext,
            cancellationToken);

        var result = await _eventService.UpdateAsync(
            eventId,
            user!.UserId,
            request,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("{eventId:guid}/close")]
    public Task<IActionResult> Close(Guid eventId, CancellationToken cancellationToken) =>
        // Closing is an owner-only lifecycle transition; the service validates
        // that the current state is Active.
        ExecuteLifecycleAsync(
            eventId,
            (ownerUserId, token) => _eventService.CloseAsync(eventId, ownerUserId, token),
            cancellationToken);

    [HttpPost("{eventId:guid}/postpone")]
    public Task<IActionResult> Postpone(
        Guid eventId,
        EventPostponeRequest request,
        CancellationToken cancellationToken) =>
        // The reason is validated by model binding; status transition rules are
        // enforced by EventService.
        ExecuteLifecycleAsync(
            eventId,
            (ownerUserId, token) => _eventService.PostponeAsync(eventId, ownerUserId, token),
            cancellationToken);

    [HttpPost("{eventId:guid}/cancel")]
    public Task<IActionResult> Cancel(
        Guid eventId,
        EventCancelRequest request,
        CancellationToken cancellationToken) =>
        // Cancellation preserves registrations and changes only the event state.
        ExecuteLifecycleAsync(
            eventId,
            (ownerUserId, token) => _eventService.CancelAsync(eventId, ownerUserId, token),
            cancellationToken);

    [HttpPost("{eventId:guid}/reschedule")]
    public async Task<IActionResult> Reschedule(
        Guid eventId,
        EventRescheduleRequest request,
        CancellationToken cancellationToken)
    {
        // Rescheduling has its own request because it changes schedule and
        // optionally venue, then reactivates a postponed event.
        var decision = await _authorizationService.RequireEventOwnerAsync(
            HttpContext, eventId, cancellationToken);
        if (!decision.Allowed)
        {
            return this.ToActionResult(decision);
        }

        // The request DTO has already passed automatic validation at this point.
        var (_, user) = await _authorizationService.RequireUserAsync(
            HttpContext, cancellationToken);
        return ToActionResult(await _eventService.RescheduleAsync(
            eventId, user!.UserId, request, cancellationToken));
    }

    private async Task<IActionResult> ExecuteLifecycleAsync(
        Guid eventId,
        Func<Guid, CancellationToken, Task<EventServiceResult<EventResponse>>> action,
        CancellationToken cancellationToken)
    {
        // This helper keeps authorization behavior identical for close,
        // postpone, and cancel endpoints.
        var decision = await _authorizationService.RequireEventOwnerAsync(
            HttpContext, eventId, cancellationToken);
        if (!decision.Allowed)
        {
            return this.ToActionResult(decision);
        }

        // The callback lets each endpoint select its specific service operation.
        var (_, user) = await _authorizationService.RequireUserAsync(
            HttpContext, cancellationToken);
        return ToActionResult(await action(user!.UserId, cancellationToken));
    }

    private IActionResult ToActionResult(
        EventServiceResult<EventResponse> result)
    {
        if (result.Succeeded)
        {
            // Successful service results are converted into HTTP 200 responses.
            return Ok(result.Value);
        }

        // Service errors are translated into stable Problem Details responses.
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
                Title = "Event conflict",
                Detail = result.Error.Detail
            });
    }
}
