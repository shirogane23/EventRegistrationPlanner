using System.ComponentModel.DataAnnotations;

namespace EventFlow.Api.Dtos.Events;

public sealed class EventCreateRequest : IValidatableObject
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string EventType { get; init; } = string.Empty;

    [Required, StringLength(4000, MinimumLength = 1)]
    public string Description { get; init; } = string.Empty;

    [Required]
    public Guid? VenueId { get; init; }

    [Required]
    public DateTime? StartUtc { get; init; }

    [Required]
    public DateTime? EndUtc { get; init; }

    [Required]
    public DateTime? RegistrationDeadlineUtc { get; init; }

    [Range(1, int.MaxValue)]
    public int Capacity { get; init; }

    [Required, RegularExpression("Public|Unlisted")]
    public string Visibility { get; init; } = string.Empty;

    public bool ApprovalRequired { get; init; }

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
