using EventFlow.Api.Models;

namespace EventFlow.Api.Services;

public interface INotificationService
{
    Task SendConfirmationAsync(Registration registration, CancellationToken cancellationToken);
    Task SendRegistrationCancellationAsync(Registration registration, CancellationToken cancellationToken);
    Task SendEventUpdateAsync(Event currentEvent, IReadOnlyCollection<Registration> registrations, string updateType, CancellationToken cancellationToken);
}

public sealed class LoggingNotificationService : INotificationService
{
    private readonly ILogger<LoggingNotificationService> _logger;

    public LoggingNotificationService(ILogger<LoggingNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendConfirmationAsync(Registration registration, CancellationToken cancellationToken)
    {
        if (registration.Status == "Confirmed" &&
            !string.IsNullOrWhiteSpace(registration.ConfirmationReference))
        {
            _logger.LogInformation(
                "EventFlow notification Confirmation queued for UserId {UserId}, EventId {EventId}, Reference {ConfirmationReference}.",
                registration.UserId, registration.EventId, registration.ConfirmationReference);
        }

        return Task.CompletedTask;
    }

    public Task SendRegistrationCancellationAsync(Registration registration, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "EventFlow notification RegistrationCancellation queued for UserId {UserId}, EventId {EventId}, Reason {Reason}.",
            registration.UserId, registration.EventId, registration.DecisionReason);
        return Task.CompletedTask;
    }

    public Task SendEventUpdateAsync(
        Event currentEvent,
        IReadOnlyCollection<Registration> registrations,
        string updateType,
        CancellationToken cancellationToken)
    {
        foreach (var registration in registrations)
        {
            _logger.LogInformation(
                "EventFlow notification {UpdateType} queued for UserId {UserId}, EventId {EventId}, RegistrationStatus {Status}.",
                updateType, registration.UserId, currentEvent.EventId, registration.Status);
        }

        return Task.CompletedTask;
    }
}
