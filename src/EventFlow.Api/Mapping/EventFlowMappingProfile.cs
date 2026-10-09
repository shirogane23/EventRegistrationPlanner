using AutoMapper;
using EventFlow.Api.Dtos.Events;
using EventFlow.Api.Dtos.Organizers;
using EventFlow.Api.Dtos.Registrations;
using EventFlow.Api.Models;

namespace EventFlow.Api.Mapping;

public sealed class EventFlowMappingProfile : Profile
{
    public EventFlowMappingProfile()
    {
        CreateMap<EventCreateRequest, Event>()
            .ForMember(destination => destination.EventId, options => options.Ignore())
            .ForMember(destination => destination.OwnerUserId, options => options.Ignore())
            .ForMember(destination => destination.Status, options => options.Ignore())
            .ForMember(destination => destination.CreatedUtc, options => options.Ignore())
            .ForMember(destination => destination.UpdatedUtc, options => options.Ignore())
            .ForMember(destination => destination.OwnerUser, options => options.Ignore())
            .ForMember(destination => destination.Registration, options => options.Ignore())
            .ForMember(destination => destination.Venue, options => options.Ignore());

        CreateMap<EventUpdateRequest, Event>()
            .ForMember(destination => destination.EventId, options => options.Ignore())
            .ForMember(destination => destination.OwnerUserId, options => options.Ignore())
            .ForMember(destination => destination.VenueId, options => options.Ignore())
            .ForMember(destination => destination.StartUtc, options => options.Ignore())
            .ForMember(destination => destination.EndUtc, options => options.Ignore())
            .ForMember(destination => destination.RegistrationDeadlineUtc, options => options.Ignore())
            .ForMember(destination => destination.Capacity, options => options.Ignore())
            .ForMember(destination => destination.Visibility, options => options.Ignore())
            .ForMember(destination => destination.ApprovalRequired, options => options.Ignore())
            .ForMember(destination => destination.Status, options => options.Ignore())
            .ForMember(destination => destination.CreatedUtc, options => options.Ignore())
            .ForMember(destination => destination.UpdatedUtc, options => options.Ignore())
            .ForMember(destination => destination.OwnerUser, options => options.Ignore())
            .ForMember(destination => destination.Registration, options => options.Ignore())
            .ForMember(destination => destination.Venue, options => options.Ignore());

        CreateMap<Event, EventResponse>()
            .ForMember(destination => destination.Venue, options => options.MapFrom(source => source.Venue))
            .ForMember(destination => destination.ActiveRegistrationCount,
                options => options.MapFrom(source => source.Registration.Count(
                    registration => registration.Status == "Pending" ||
                                    registration.Status == "Confirmed")));

        CreateMap<Event, EventSummaryResponse>()
            .ForMember(destination => destination.VenueName, options => options.MapFrom(source => source.Venue.Name))
            .ForMember(destination => destination.ActiveRegistrationCount,
                options => options.MapFrom(source => source.Registration.Count(
                    registration => registration.Status == "Pending" ||
                                    registration.Status == "Confirmed")));

        CreateMap<Event, OrganizerEventResponse>()
            .IncludeBase<Event, EventResponse>()
            .ForMember(destination => destination.PendingRegistrationCount,
                options => options.MapFrom(source => source.Registration.Count(
                    registration => registration.Status == "Pending")))
            .ForMember(destination => destination.ConfirmedRegistrationCount,
                options => options.MapFrom(source => source.Registration.Count(
                    registration => registration.Status == "Confirmed")));

        CreateMap<Venue, VenueResponse>();

        CreateMap<Registration, RegistrationResponse>()
            .ForMember(destination => destination.EventTitle, options => options.MapFrom(source => source.Event.Title))
            .ForMember(destination => destination.EventStartUtc, options => options.MapFrom(source => source.Event.StartUtc))
            .ForMember(destination => destination.EventEndUtc, options => options.MapFrom(source => source.Event.EndUtc))
            .ForMember(destination => destination.VenueName, options => options.MapFrom(source => source.Event.Venue.Name));

        CreateMap<Registration, OrganizerRegistrationResponse>()
            .ForMember(destination => destination.AttendeeDisplayName,
                options => options.MapFrom(source => source.User.DisplayName))
            .ForMember(destination => destination.AttendeeEmail,
                options => options.MapFrom(source => source.User.Email));
    }
}
