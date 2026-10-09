using EventFlow.Api.Auth;
using EventFlow.Api.Dtos.Events;
using EventFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventFlow.Api.Controllers;

[ApiController]
[Route("api/events")]
public sealed class EventsController : ControllerBase
{
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
        Ok(await _eventService.SearchPublicAsync(title, cancellationToken));

    [HttpGet("{eventId:guid}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        Guid eventId,
        CancellationToken cancellationToken)
    {
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
        var (decision, user) = await _authorizationService.RequireRoleAsync(
            HttpContext,
            DemoIdentityConstants.OrganizerRole,
            cancellationToken);

        if (!decision.Allowed || user is null)
        {
            return this.ToActionResult(decision);
        }

        var result = await _eventService.CreateAsync(
            request,
            user.UserId,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ToActionResult(result);
        }

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
        var (decision, user) = await _authorizationService.RequireRoleAsync(
            HttpContext,
            DemoIdentityConstants.OrganizerRole,
            cancellationToken);

        if (!decision.Allowed || user is null)
        {
            return this.ToActionResult(decision);
        }

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
        var decision = await _authorizationService.RequireEventOwnerAsync(
            HttpContext,
            eventId,
            cancellationToken);

        if (!decision.Allowed)
        {
            return this.ToActionResult(decision);
        }

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

    private IActionResult ToActionResult(
        EventServiceResult<EventResponse> result)
    {
        if (result.Succeeded)
        {
            return Ok(result.Value);
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
                Title = "Event conflict",
                Detail = result.Error.Detail
            });
    }
}
