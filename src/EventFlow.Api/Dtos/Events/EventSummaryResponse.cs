namespace EventFlow.Api.Dtos.Events;

public sealed class EventSummaryResponse
{
    public Guid EventId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string VenueName { get; init; } = string.Empty;
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
    public int Capacity { get; init; }
    public int ActiveRegistrationCount { get; init; }
    public bool ApprovalRequired { get; init; }
}
