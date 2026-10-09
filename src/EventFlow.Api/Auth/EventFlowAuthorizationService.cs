using EventFlow.Api.Data;
using EventFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Api.Auth;

public sealed class EventFlowAuthorizationService
{
    private readonly IDemoIdentityResolver _identityResolver;
    private readonly EventFlowDbContext _dbContext;

    public EventFlowAuthorizationService(
        IDemoIdentityResolver identityResolver,
        EventFlowDbContext dbContext)
    {
        _identityResolver = identityResolver;
        _dbContext = dbContext;
    }

    public async Task<(AuthorizationDecision Decision, User? User)> RequireUserAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var resolution = await _identityResolver.ResolveAsync(
            httpContext,
            cancellationToken);

        if (resolution.IsAuthenticated)
        {
            return (AuthorizationDecision.Allow(), resolution.User);
        }

        var detail = resolution.Failure == DemoIdentityFailure.MissingHeader
            ? $"The {DemoIdentityConstants.HeaderName} header is required."
            : "The supplied demo identity does not match a seeded user.";

        return (AuthorizationDecision.Unauthorized(detail), null);
    }

    public async Task<(AuthorizationDecision Decision, User? User)> RequireRoleAsync(
        HttpContext httpContext,
        string role,
        CancellationToken cancellationToken)
    {
        var (authenticationDecision, user) = await RequireUserAsync(
            httpContext,
            cancellationToken);

        if (!authenticationDecision.Allowed || user is null)
        {
            return (authenticationDecision, null);
        }

        return user.Role == role
            ? (authenticationDecision, user)
            : (
                AuthorizationDecision.Forbidden(
                    $"The {role} role is required for this operation."),
                user);
    }

    public async Task<AuthorizationDecision> RequireEventOwnerAsync(
        HttpContext httpContext,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var (decision, user) = await RequireRoleAsync(
            httpContext,
            DemoIdentityConstants.OrganizerRole,
            cancellationToken);

        if (!decision.Allowed || user is null)
        {
            return decision;
        }

        var ownsEvent = await _dbContext.Event
            .AsNoTracking()
            .AnyAsync(
                currentEvent => currentEvent.EventId == eventId &&
                                currentEvent.OwnerUserId == user.UserId,
                cancellationToken);

        return ownsEvent
            ? AuthorizationDecision.Allow()
            : AuthorizationDecision.Forbidden(
                "Only the event owner may perform this operation.");
    }

    public async Task<AuthorizationDecision> RequireRegistrationOwnerAsync(
        HttpContext httpContext,
        Guid registrationId,
        CancellationToken cancellationToken)
    {
        var (decision, user) = await RequireUserAsync(
            httpContext,
            cancellationToken);

        if (!decision.Allowed || user is null)
        {
            return decision;
        }

        var ownsRegistration = await _dbContext.Registration
            .AsNoTracking()
            .AnyAsync(
                registration => registration.RegistrationId == registrationId &&
                                registration.UserId == user.UserId,
                cancellationToken);

        return ownsRegistration
            ? AuthorizationDecision.Allow()
            : AuthorizationDecision.Forbidden(
                "Only the registration owner may perform this operation.");
    }

    public async Task<AuthorizationDecision> RequireEventRegistrationOwnerAsync(
        HttpContext httpContext,
        Guid registrationId,
        CancellationToken cancellationToken)
    {
        var (decision, user) = await RequireRoleAsync(
            httpContext,
            DemoIdentityConstants.OrganizerRole,
            cancellationToken);

        if (!decision.Allowed || user is null)
        {
            return decision;
        }

        var ownsEvent = await _dbContext.Registration
            .AsNoTracking()
            .AnyAsync(
                registration => registration.RegistrationId == registrationId &&
                                registration.Event.OwnerUserId == user.UserId,
                cancellationToken);

        return ownsEvent
            ? AuthorizationDecision.Allow()
            : AuthorizationDecision.Forbidden(
                "Only the event owner may manage this registration.");
    }
}
