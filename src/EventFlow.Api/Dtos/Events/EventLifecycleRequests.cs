using System.ComponentModel.DataAnnotations;

namespace EventFlow.Api.Dtos.Events;

public sealed class EventPostponeRequest
{
    [Required, StringLength(1000, MinimumLength = 1)]
    public string Reason { get; init; } = string.Empty;
}

public sealed class EventCancelRequest
{
    [Required, StringLength(1000, MinimumLength = 1)]
    public string Reason { get; init; } = string.Empty;
}

public sealed class EventRescheduleRequest : IValidatableObject
{
    [Required]
    public DateTime? StartUtc { get; init; }

    [Required]
    public DateTime? EndUtc { get; init; }

    [Required]
    public DateTime? RegistrationDeadlineUtc { get; init; }

    public Guid? VenueId { get; init; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (StartUtc is null || EndUtc is null ||
            RegistrationDeadlineUtc is null)
        {
            yield break;
        }

        if (StartUtc <= DateTime.UtcNow)
        {
            yield return new ValidationResult(
                "StartUtc must be in the future.",
                new[] { nameof(StartUtc) });
        }

        if (EndUtc <= StartUtc)
        {
            yield return new ValidationResult(
                "EndUtc must be after StartUtc.",
                new[] { nameof(EndUtc), nameof(StartUtc) });
        }

        if (RegistrationDeadlineUtc > StartUtc)
        {
            yield return new ValidationResult(
                "RegistrationDeadlineUtc must not be after StartUtc.",
                new[] { nameof(RegistrationDeadlineUtc), nameof(StartUtc) });
        }
    }
}
