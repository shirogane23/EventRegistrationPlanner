using System.ComponentModel.DataAnnotations;
using EventFlow.Api.Dtos.Events;
using EventFlow.Api.Dtos.Registrations;

namespace EventFlow.Api.Tests;

public sealed class ContractValidationTests
{
    [Fact]
    public void EventCreateRejectsEndBeforeStart()
    {
        var request = new EventCreateRequest
        {
            Title = "Test",
            EventType = "Workshop",
            Description = "Description",
            VenueId = Guid.NewGuid(),
            StartUtc = DateTime.UtcNow.AddDays(1),
            EndUtc = DateTime.UtcNow.AddHours(1),
            RegistrationDeadlineUtc = DateTime.UtcNow
        };

        var results = Validate(request);

        Assert.Contains(results, result => result.ErrorMessage!.Contains("EndUtc"));
    }

    [Fact]
    public void RegistrationActionRequiresReason()
    {
        var results = Validate(new RegistrationActionRequest());

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(RegistrationActionRequest.Reason)));
    }

    private static IReadOnlyList<ValidationResult> Validate(object value)
    {
        var context = new ValidationContext(value);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(value, context, results, validateAllProperties: true);
        if (value is IValidatableObject validatable)
        {
            results.AddRange(validatable.Validate(context));
        }

        return results;
    }
}
