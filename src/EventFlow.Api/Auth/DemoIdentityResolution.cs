using EventFlow.Api.Models;

namespace EventFlow.Api.Auth;

public sealed record DemoIdentityResolution(
    User? User,
    DemoIdentityFailure Failure)
{
    public bool IsAuthenticated => User is not null;
}

public enum DemoIdentityFailure
{
    None,
    MissingHeader,
    UnknownIdentity
}
