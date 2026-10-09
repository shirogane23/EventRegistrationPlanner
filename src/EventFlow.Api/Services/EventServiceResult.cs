namespace EventFlow.Api.Services;

public sealed record EventServiceResult<T>(
    T? Value,
    EventServiceError? Error = null)
{
    public bool Succeeded => Error is null;

    public static EventServiceResult<T> Success(T value) =>
        new(value);

    public static EventServiceResult<T> Failure(
        EventServiceErrorCode code,
        string detail) =>
        new(default, new EventServiceError(code, detail));
}

public sealed record EventServiceError(
    EventServiceErrorCode Code,
    string Detail);

public enum EventServiceErrorCode
{
    NotFound,
    Conflict
}
