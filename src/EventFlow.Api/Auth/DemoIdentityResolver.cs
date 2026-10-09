using EventFlow.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Api.Auth;

public sealed class DemoIdentityResolver : IDemoIdentityResolver
{
    private readonly EventFlowDbContext _dbContext;

    public DemoIdentityResolver(EventFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DemoIdentityResolution> ResolveAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var identity = httpContext.Request.Headers[
            DemoIdentityConstants.HeaderName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(identity))
        {
            return new DemoIdentityResolution(
                null,
                DemoIdentityFailure.MissingHeader);
        }

        var user = await _dbContext.User
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.DemoIdentity == identity.Trim(),
                cancellationToken);

        return user is null
            ? new DemoIdentityResolution(null, DemoIdentityFailure.UnknownIdentity)
            : new DemoIdentityResolution(user, DemoIdentityFailure.None);
    }
}
