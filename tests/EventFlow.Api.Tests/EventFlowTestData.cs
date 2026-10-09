using AutoMapper;
using EventFlow.Api.Data;
using EventFlow.Api.Mapping;
using EventFlow.Api.Models;
using EventFlow.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Moq;

namespace EventFlow.Api.Tests;

internal static class EventFlowTestData
{
    public static EventFlowDbContext CreateContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        connection.CreateFunction("sysutcdatetime", () => DateTime.UtcNow);
        connection.CreateFunction("newsequentialid", () => Guid.NewGuid());

        var options = new DbContextOptionsBuilder<EventFlowDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new EventFlowDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static IMapper CreateMapper() =>
        new MapperConfiguration(configuration =>
            configuration.AddProfile<EventFlowMappingProfile>())
        .CreateMapper();

    public static Mock<INotificationService> CreateNotifications() =>
        new();

    public static Event CreateEvent(
        Guid ownerUserId,
        Guid venueId,
        string status = "Active",
        bool approvalRequired = false) =>
        new()
        {
            EventId = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            VenueId = venueId,
            Title = "Test event",
            EventType = "Workshop",
            Description = "Test event description",
            StartUtc = DateTime.UtcNow.AddDays(10),
            EndUtc = DateTime.UtcNow.AddDays(10).AddHours(2),
            RegistrationDeadlineUtc = DateTime.UtcNow.AddDays(9),
            Capacity = 2,
            Visibility = "Public",
            ApprovalRequired = approvalRequired,
            Status = status,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };
}
