using System.ComponentModel.DataAnnotations;

namespace EventFlow.Api.Dtos.Registrations;

public sealed class RegistrationActionRequest
{
    [Required, StringLength(1000, MinimumLength = 1)]
    public string Reason { get; init; } = string.Empty;
}
