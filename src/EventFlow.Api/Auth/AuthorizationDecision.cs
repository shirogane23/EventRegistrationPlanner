namespace EventFlow.Api.Auth;

public sealed record AuthorizationDecision(
    bool Allowed,
    int StatusCode,
    string? ErrorCode = null,
    string? Detail = null)
{
    public static AuthorizationDecision Allow() =>
        new(true, StatusCodes.Status200OK);

    public static AuthorizationDecision Unauthorized(string detail) =>
        new(false, StatusCodes.Status401Unauthorized, "unauthorized", detail);

    public static AuthorizationDecision Forbidden(string detail) =>
        new(false, StatusCodes.Status403Forbidden, "forbidden", detail);
}
