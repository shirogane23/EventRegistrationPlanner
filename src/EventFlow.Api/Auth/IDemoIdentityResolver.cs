namespace EventFlow.Api.Auth;

public interface IDemoIdentityResolver
{
    Task<DemoIdentityResolution> ResolveAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken);
}
