namespace EventFlow.Api.Dtos.Events;

public class EventResponse
{
    public Guid EventId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public VenueResponse Venue { get; init; } = new();
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
    public DateTime RegistrationDeadlineUtc { get; init; }
    public int Capacity { get; init; }
    public int ActiveRegistrationCount { get; init; }
    public string Visibility { get; init; } = string.Empty;
    public bool ApprovalRequired { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed class VenueResponse
{
    public Guid VenueId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public int Capacity { get; init; }
}
