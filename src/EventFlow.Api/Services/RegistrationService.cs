using AutoMapper;
using EventFlow.Api.Data;
using EventFlow.Api.Dtos.Registrations;
using EventFlow.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace EventFlow.Api.Services;

public sealed class RegistrationService
{
    private const string ActiveStatus = "Active";
    private const string PendingStatus = "Pending";
    private const string ConfirmedStatus = "Confirmed";
    private const string CancelledStatus = "Cancelled";

    private readonly EventFlowDbContext _dbContext;
    private readonly IMapper _mapper;

    public RegistrationService(EventFlowDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<EventServiceResult<RegistrationResponse>> CreateAsync(
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var currentEvent = await _dbContext.Event
            .SingleOrDefaultAsync(
                candidate => candidate.EventId == eventId,
                cancellationToken);

        if (currentEvent is null)
        {
            return Failure(
                EventServiceErrorCode.NotFound,
                "The event was not found.");
        }

        var now = DateTime.UtcNow;
        if (currentEvent.Status != ActiveStatus ||
            currentEvent.StartUtc <= now ||
            currentEvent.RegistrationDeadlineUtc < now)
        {
            return Failure(
                EventServiceErrorCode.Conflict,
                "The event is not eligible for registration.");
        }

        var duplicateExists = await _dbContext.Registration
            .AnyAsync(
                registration => registration.EventId == eventId &&
                                registration.UserId == userId,
                cancellationToken);

        if (duplicateExists)
        {
            return Failure(
                EventServiceErrorCode.Conflict,
                "The user already has a registration attempt for this event.");
        }

        var activeRegistrationCount = await _dbContext.Registration
            .CountAsync(
                registration => registration.EventId == eventId &&
                                (registration.Status == PendingStatus ||
                                 registration.Status == ConfirmedStatus),
                cancellationToken);

        if (activeRegistrationCount >= currentEvent.Capacity)
        {
            return Failure(
                EventServiceErrorCode.Conflict,
                "The event has no remaining capacity.");
        }

        var status = currentEvent.ApprovalRequired
            ? PendingStatus
            : ConfirmedStatus;

        var registration = new Registration
        {
            EventId = eventId,
            UserId = userId,
            Status = status,
            ConfirmationReference = status == ConfirmedStatus
                ? CreateConfirmationReference()
                : null
        };

        _dbContext.Registration.Add(registration);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(
            registration.RegistrationId,
            userId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<RegistrationResponse>> GetCurrentAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var registrations = await GetRegistrationQuery()
            .Where(registration =>
                registration.UserId == userId &&
                (registration.Status == PendingStatus ||
                 registration.Status == ConfirmedStatus))
            .OrderBy(registration => registration.Event.StartUtc)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IReadOnlyList<RegistrationResponse>>(registrations);
    }

    public async Task<EventServiceResult<RegistrationResponse>> CancelAsync(
        Guid registrationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var registration = await _dbContext.Registration
            .Include(currentRegistration => currentRegistration.Event)
            .SingleOrDefaultAsync(
                candidate => candidate.RegistrationId == registrationId &&
                              candidate.UserId == userId,
                cancellationToken);

        if (registration is null)
        {
            return Failure(
                EventServiceErrorCode.NotFound,
                "The registration was not found.");
        }

        if (registration.Status != PendingStatus &&
            registration.Status != ConfirmedStatus)
        {
            return Failure(
                EventServiceErrorCode.Conflict,
                "Only Pending or Confirmed registrations may be cancelled.");
        }

        if (registration.Event.Status == "Cancelled")
        {
            return Failure(
                EventServiceErrorCode.Conflict,
                "Registrations cannot be cancelled through this operation after event cancellation.");
        }

        registration.Status = CancelledStatus;
        registration.DecisionReason = "Cancelled by attendee.";
        registration.UpdatedUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(
            registrationId,
            userId,
            cancellationToken);
    }

    private async Task<EventServiceResult<RegistrationResponse>> GetByIdAsync(
        Guid registrationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var registration = await GetRegistrationQuery()
            .SingleOrDefaultAsync(
                candidate => candidate.RegistrationId == registrationId &&
                              candidate.UserId == userId,
                cancellationToken);

        return registration is null
            ? Failure(
                EventServiceErrorCode.NotFound,
                "The registration was not found.")
            : EventServiceResult<RegistrationResponse>.Success(
                _mapper.Map<RegistrationResponse>(registration));
    }

    private IQueryable<Registration> GetRegistrationQuery() =>
        _dbContext.Registration
            .AsNoTracking()
            .Include(registration => registration.Event)
            .ThenInclude(currentEvent => currentEvent.Venue);

    private static string CreateConfirmationReference() =>
        $"EF-{Guid.NewGuid():N}".ToUpperInvariant();

    private static EventServiceResult<RegistrationResponse> Failure(
        EventServiceErrorCode code,
        string detail) =>
        EventServiceResult<RegistrationResponse>.Failure(code, detail);
}
