using EventFlow.Api.Services;

namespace EventFlow.Api.Tests;

public sealed class EventServiceTests
{
    [Fact]
    public async Task PostponedEventCanBeRescheduledToActive()
    {
        await using var context = EventFlowTestData.CreateContext();
        var ownerId = Guid.NewGuid();
        var venueId = Guid.NewGuid();
        context.User.Add(new EventFlow.Api.Models.User
        {
            UserId = ownerId,
            DisplayName = "Organizer",
            Email = "owner@test",
            DemoIdentity = "owner",
            Role = "Organizer"
        });
        context.Venue.Add(new EventFlow.Api.Models.Venue
        {
            VenueId = venueId,
            Name = "Venue",
            Address = "Address",
            Capacity = 20
        });
        var currentEvent = EventFlowTestData.CreateEvent(ownerId, venueId, "Postponed");
        context.Event.Add(currentEvent);
        await context.SaveChangesAsync();
        var service = new EventService(
            context,
            EventFlowTestData.CreateMapper(),
            EventFlowTestData.CreateNotifications().Object);

        var result = await service.RescheduleAsync(
            currentEvent.EventId,
            ownerId,
            new EventFlow.Api.Dtos.Events.EventRescheduleRequest
            {
                StartUtc = DateTime.UtcNow.AddDays(20),
                EndUtc = DateTime.UtcNow.AddDays(20).AddHours(2),
                RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(19),
                VenueId = venueId
            },
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Active", result.Value!.Status);
    }
}
