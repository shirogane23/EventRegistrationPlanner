using AutoMapper;
using EventFlow.Api.Data;
using EventFlow.Api.Dtos.Events;
using EventFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Api.Services;

public sealed class EventService
{
    private const string ActiveStatus = "Active";
    private const string ClosedStatus = "Closed";
    private const string PostponedStatus = "Postponed";
    private const string CancelledStatus = "Cancelled";
    private const string PublicVisibility = "Public";
    private readonly EventFlowDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;

    public EventService(
        EventFlowDbContext dbContext,
        IMapper mapper,
        INotificationService notificationService)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _notificationService = notificationService;
    }

    public async Task<IReadOnlyList<EventSummaryResponse>> SearchPublicAsync(
        string? title,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var query = _dbContext.Event
            .AsNoTracking()
            .Include(currentEvent => currentEvent.Venue)
            .Include(currentEvent => currentEvent.Registration)
            .Where(currentEvent =>
                currentEvent.Visibility == PublicVisibility &&
                currentEvent.Status == ActiveStatus &&
                currentEvent.StartUtc > now &&
                currentEvent.RegistrationDeadlineUtc >= now);

        if (!string.IsNullOrWhiteSpace(title))
        {
            var search = title.Trim();
            query = query.Where(currentEvent =>
                currentEvent.Title.Contains(search));
        }

        var events = await query
            .OrderBy(currentEvent => currentEvent.StartUtc)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IReadOnlyList<EventSummaryResponse>>(events);
    }

    public async Task<EventServiceResult<EventResponse>> GetPublicAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var currentEvent = await GetEventQuery()
            .SingleOrDefaultAsync(
                candidate => candidate.EventId == eventId,
                cancellationToken);

        if (currentEvent is null ||
            (currentEvent.Visibility != PublicVisibility &&
             currentEvent.Status == "Cancelled"))
        {
            return EventServiceResult<EventResponse>.Failure(
                EventServiceErrorCode.NotFound,
                "The event was not found.");
        }

        return EventServiceResult<EventResponse>.Success(
            _mapper.Map<EventResponse>(currentEvent));
    }

    public async Task<EventServiceResult<EventResponse>> GetOwnedAsync(
        Guid eventId,
        Guid ownerUserId,
        CancellationToken cancellationToken)
    {
        var currentEvent = await GetEventQuery()
            .SingleOrDefaultAsync(
                candidate => candidate.EventId == eventId &&
                              candidate.OwnerUserId == ownerUserId,
                cancellationToken);

        return currentEvent is null
            ? EventServiceResult<EventResponse>.Failure(
                EventServiceErrorCode.NotFound,
                "The owned event was not found.")
            : EventServiceResult<EventResponse>.Success(
                _mapper.Map<EventResponse>(currentEvent));
    }

    public async Task<EventServiceResult<EventResponse>> CreateAsync(
        EventCreateRequest request,
        Guid ownerUserId,
        CancellationToken cancellationToken)
    {
        var venue = await _dbContext.Venue
            .SingleOrDefaultAsync(
                candidate => candidate.VenueId == request.VenueId,
                cancellationToken);

        if (venue is null)
        {
            return EventServiceResult<EventResponse>.Failure(
                EventServiceErrorCode.NotFound,
                "The selected venue was not found.");
        }

        if (request.Capacity > venue.Capacity)
        {
            return EventServiceResult<EventResponse>.Failure(
                EventServiceErrorCode.Conflict,
                "Event capacity cannot exceed venue capacity.");
        }

        if (await HasOverlappingActiveEventAsync(
                request.VenueId!.Value,
                request.StartUtc!.Value,
                request.EndUtc!.Value,
                cancellationToken))
        {
            return EventServiceResult<EventResponse>.Failure(
                EventServiceErrorCode.Conflict,
                "The selected venue already has an overlapping active event.");
        }

        var currentEvent = new Event
        {
            OwnerUserId = ownerUserId,
            VenueId = request.VenueId.Value,
            Title = request.Title.Trim(),
            EventType = request.EventType.Trim(),
            Description = request.Description.Trim(),
            StartUtc = DateTime.SpecifyKind(request.StartUtc.Value, DateTimeKind.Utc),
            EndUtc = DateTime.SpecifyKind(request.EndUtc.Value, DateTimeKind.Utc),
            RegistrationDeadlineUtc = DateTime.SpecifyKind(
                request.RegistrationDeadlineUtc!.Value,
                DateTimeKind.Utc),
            Capacity = request.Capacity,
            Visibility = request.Visibility,
            ApprovalRequired = request.ApprovalRequired,
            Status = ActiveStatus
        };

        _dbContext.Event.Add(currentEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
        currentEvent.Venue = venue;

        return EventServiceResult<EventResponse>.Success(
            _mapper.Map<EventResponse>(currentEvent));
    }

    public async Task<EventServiceResult<EventResponse>> UpdateAsync(
        Guid eventId,
        Guid ownerUserId,
        EventUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var currentEvent = await GetEventQuery()
            .SingleOrDefaultAsync(
                candidate => candidate.EventId == eventId &&
                              candidate.OwnerUserId == ownerUserId,
                cancellationToken);

        if (currentEvent is null)
        {
            return EventServiceResult<EventResponse>.Failure(
                EventServiceErrorCode.NotFound,
                "The owned event was not found.");
        }

        if (currentEvent.Status != ActiveStatus)
        {
            return EventServiceResult<EventResponse>.Failure(
                EventServiceErrorCode.Conflict,
                "Only Active events may receive ordinary updates.");
        }

        _mapper.Map(request, currentEvent);
        currentEvent.UpdatedUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return EventServiceResult<EventResponse>.Success(
            _mapper.Map<EventResponse>(currentEvent));
    }

    public Task<EventServiceResult<EventResponse>> CloseAsync(
        Guid eventId,
        Guid ownerUserId,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(
            eventId,
            ownerUserId,
            ActiveStatus,
            ClosedStatus,
            "Only Active events may be closed.",
            cancellationToken);

    public Task<EventServiceResult<EventResponse>> PostponeAsync(
        Guid eventId,
        Guid ownerUserId,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(
            eventId,
            ownerUserId,
            ActiveStatus,
            PostponedStatus,
            "Only Active events may be postponed.",
            cancellationToken);

    public Task<EventServiceResult<EventResponse>> CancelAsync(
        Guid eventId,
        Guid ownerUserId,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(
            eventId,
            ownerUserId,
            null,
            CancelledStatus,
            "The event is already cancelled.",
            cancellationToken);

    public async Task<EventServiceResult<EventResponse>> RescheduleAsync(
        Guid eventId,
        Guid ownerUserId,
        EventRescheduleRequest request,
        CancellationToken cancellationToken)
    {
        var currentEvent = await _dbContext.Event
            .SingleOrDefaultAsync(
                candidate => candidate.EventId == eventId &&
                              candidate.OwnerUserId == ownerUserId,
                cancellationToken);

        if (currentEvent is null)
        {
            return Failure("The owned event was not found.", EventServiceErrorCode.NotFound);
        }

        if (currentEvent.Status != PostponedStatus)
        {
            return Failure(
                "Only Postponed events may be rescheduled.",
                EventServiceErrorCode.Conflict);
        }

        var venueId = request.VenueId ?? currentEvent.VenueId;
        var venue = await _dbContext.Venue
            .SingleOrDefaultAsync(candidate => candidate.VenueId == venueId, cancellationToken);

        if (venue is null)
        {
            return Failure("The selected venue was not found.", EventServiceErrorCode.NotFound);
        }

        if (currentEvent.Capacity > venue.Capacity)
        {
            return Failure(
                "Event capacity cannot exceed venue capacity.",
                EventServiceErrorCode.Conflict);
        }

        if (await HasOverlappingActiveEventAsync(
                venueId,
                request.StartUtc!.Value,
                request.EndUtc!.Value,
                cancellationToken,
                eventId))
        {
            return Failure(
                "The selected venue already has an overlapping active event.",
                EventServiceErrorCode.Conflict);
        }

        currentEvent.VenueId = venueId;
        currentEvent.StartUtc = DateTime.SpecifyKind(request.StartUtc.Value, DateTimeKind.Utc);
        currentEvent.EndUtc = DateTime.SpecifyKind(request.EndUtc.Value, DateTimeKind.Utc);
        currentEvent.RegistrationDeadlineUtc = DateTime.SpecifyKind(
            request.RegistrationDeadlineUtc!.Value,
            DateTimeKind.Utc);
        currentEvent.Status = ActiveStatus;
        currentEvent.UpdatedUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        currentEvent.Venue = venue;
        await _notificationService.SendEventUpdateAsync(
            currentEvent,
            await GetActiveRegistrationsAsync(eventId, cancellationToken),
            "Rescheduled",
            cancellationToken);
        return EventServiceResult<EventResponse>.Success(
            _mapper.Map<EventResponse>(currentEvent));
    }

    private async Task<EventServiceResult<EventResponse>> ChangeStatusAsync(
        Guid eventId,
        Guid ownerUserId,
        string? requiredCurrentStatus,
        string targetStatus,
        string invalidStateMessage,
        CancellationToken cancellationToken)
    {
        var currentEvent = await GetEventQuery()
            .SingleOrDefaultAsync(
                candidate => candidate.EventId == eventId &&
                              candidate.OwnerUserId == ownerUserId,
                cancellationToken);

        if (currentEvent is null)
        {
            return Failure("The owned event was not found.", EventServiceErrorCode.NotFound);
        }

        if (currentEvent.Status == CancelledStatus ||
            (requiredCurrentStatus is not null &&
             currentEvent.Status != requiredCurrentStatus))
        {
            return Failure(invalidStateMessage, EventServiceErrorCode.Conflict);
        }

        currentEvent.Status = targetStatus;
        currentEvent.UpdatedUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var notificationType = targetStatus == CancelledStatus
            ? "EventCancelled"
            : targetStatus == PostponedStatus
                ? "EventPostponed"
                : "EventUpdated";
        await _notificationService.SendEventUpdateAsync(
            currentEvent,
            await GetActiveRegistrationsAsync(eventId, cancellationToken),
            notificationType,
            cancellationToken);

        return EventServiceResult<EventResponse>.Success(
            _mapper.Map<EventResponse>(currentEvent));
    }

    private static EventServiceResult<EventResponse> Failure(
        string detail,
        EventServiceErrorCode code) =>
        EventServiceResult<EventResponse>.Failure(code, detail);

    private Task<List<Registration>> GetActiveRegistrationsAsync(
        Guid eventId,
        CancellationToken cancellationToken) =>
        _dbContext.Registration
            .Where(registration =>
                registration.EventId == eventId &&
                (registration.Status == "Pending" ||
                 registration.Status == "Confirmed"))
            .ToListAsync(cancellationToken);

    private IQueryable<Event> GetEventQuery() =>
        _dbContext.Event
            .AsNoTracking()
            .Include(currentEvent => currentEvent.Venue)
            .Include(currentEvent => currentEvent.Registration);

    private Task<bool> HasOverlappingActiveEventAsync(
        Guid venueId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken,
        Guid? excludedEventId = null) =>
        _dbContext.Event.AnyAsync(
            currentEvent =>
                currentEvent.VenueId == venueId &&
                currentEvent.Status == ActiveStatus &&
                (excludedEventId == null || currentEvent.EventId != excludedEventId) &&
                currentEvent.StartUtc < endUtc &&
                startUtc < currentEvent.EndUtc,
            cancellationToken);
}
