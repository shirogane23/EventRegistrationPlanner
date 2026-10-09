using EventFlow.Api.Models;
using EventFlow.Api.Services;
using Moq;

namespace EventFlow.Api.Tests;

public sealed class RegistrationServiceTests
{
    [Fact]
    public async Task ApprovalRequiredEventCreatesPendingRegistrationWithoutConfirmation()
    {
        await using var context = EventFlowTestData.CreateContext();
        var attendeeId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var venueId = Guid.NewGuid();
        context.User.AddRange(
            new User { UserId = attendeeId, DisplayName = "Attendee", Email = "a@test", DemoIdentity = "a", Role = "Attendee" },
            new User { UserId = organizerId, DisplayName = "Organizer", Email = "o@test", DemoIdentity = "o", Role = "Organizer" });
        context.Venue.Add(new Venue { VenueId = venueId, Name = "Venue", Address = "Address", Capacity = 20 });
        context.Event.Add(EventFlowTestData.CreateEvent(organizerId, venueId, approvalRequired: true));
        await context.SaveChangesAsync();
        var currentEvent = context.Event.Single();
        var notifications = EventFlowTestData.CreateNotifications();
        var service = new RegistrationService(context, EventFlowTestData.CreateMapper(), notifications.Object);

        var result = await service.CreateAsync(currentEvent.EventId, attendeeId, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Pending", result.Value!.Status);
        Assert.Null(result.Value.ConfirmationReference);
        notifications.Verify(
            notification => notification.SendConfirmationAsync(
                It.IsAny<Registration>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DuplicateRegistrationIsRejected()
    {
        await using var context = EventFlowTestData.CreateContext();
        var attendeeId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var venueId = Guid.NewGuid();
        context.User.AddRange(
            new User { UserId = attendeeId, DisplayName = "Attendee", Email = "a@test", DemoIdentity = "a", Role = "Attendee" },
            new User { UserId = organizerId, DisplayName = "Organizer", Email = "o@test", DemoIdentity = "o", Role = "Organizer" });
        context.Venue.Add(new Venue { VenueId = venueId, Name = "Venue", Address = "Address", Capacity = 20 });
        var currentEvent = EventFlowTestData.CreateEvent(organizerId, venueId);
        context.Event.Add(currentEvent);
        await context.SaveChangesAsync();
        var service = new RegistrationService(
            context,
            EventFlowTestData.CreateMapper(),
            EventFlowTestData.CreateNotifications().Object);

        var first = await service.CreateAsync(currentEvent.EventId, attendeeId, CancellationToken.None);
        var second = await service.CreateAsync(currentEvent.EventId, attendeeId, CancellationToken.None);

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Equal(EventServiceErrorCode.Conflict, second.Error!.Code);
    }
}
