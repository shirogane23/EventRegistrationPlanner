using AutoMapper;
using EventFlow.Api.Data;
using EventFlow.Api.Dtos.Events;
using EventFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Api.Services;

public sealed class EventService
{
    private const string ActiveStatus = "Active";
    private const string PublicVisibility = "Public";
    private readonly EventFlowDbContext _dbContext;
    private readonly IMapper _mapper;

    public EventService(EventFlowDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
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

    private IQueryable<Event> GetEventQuery() =>
        _dbContext.Event
            .AsNoTracking()
            .Include(currentEvent => currentEvent.Venue)
            .Include(currentEvent => currentEvent.Registration);

    private Task<bool> HasOverlappingActiveEventAsync(
        Guid venueId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken) =>
        _dbContext.Event.AnyAsync(
            currentEvent =>
                currentEvent.VenueId == venueId &&
                currentEvent.Status == ActiveStatus &&
                currentEvent.StartUtc < endUtc &&
                startUtc < currentEvent.EndUtc,
            cancellationToken);
}
