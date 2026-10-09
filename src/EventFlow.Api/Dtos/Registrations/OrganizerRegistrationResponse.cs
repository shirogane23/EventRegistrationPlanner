namespace EventFlow.Api.Dtos.Registrations;

public sealed class OrganizerRegistrationResponse
{
    public Guid RegistrationId { get; init; }
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public string AttendeeDisplayName { get; init; } = string.Empty;
    public string AttendeeEmail { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? ConfirmationReference { get; init; }
    public string? DecisionReason { get; init; }
    public DateTime CreatedUtc { get; init; }
    public DateTime UpdatedUtc { get; init; }
}
