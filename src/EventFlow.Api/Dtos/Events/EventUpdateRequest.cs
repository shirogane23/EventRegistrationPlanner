using System.ComponentModel.DataAnnotations;

namespace EventFlow.Api.Dtos.Events;

public sealed class EventUpdateRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string EventType { get; init; } = string.Empty;

    [Required, StringLength(4000, MinimumLength = 1)]
    public string Description { get; init; } = string.Empty;
}
