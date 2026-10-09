namespace EventFlow.Api.Dtos.Registrations;

public sealed class RegistrationResponse
{
    public Guid RegistrationId { get; init; }
    public Guid EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public DateTime EventStartUtc { get; init; }
    public DateTime EventEndUtc { get; init; }
    public string VenueName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? ConfirmationReference { get; init; }
    public string? DecisionReason { get; init; }
    public DateTime CreatedUtc { get; init; }
    public DateTime UpdatedUtc { get; init; }
}
