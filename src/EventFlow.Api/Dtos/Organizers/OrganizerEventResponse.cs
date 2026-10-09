using EventFlow.Api.Dtos.Events;

namespace EventFlow.Api.Dtos.Organizers;

public sealed class OrganizerEventResponse : EventResponse
{
    public Guid OwnerUserId { get; init; }
    public int PendingRegistrationCount { get; init; }
    public int ConfirmedRegistrationCount { get; init; }
}
