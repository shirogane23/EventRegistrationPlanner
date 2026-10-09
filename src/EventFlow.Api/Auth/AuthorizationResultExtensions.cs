using Microsoft.AspNetCore.Mvc;

namespace EventFlow.Api.Auth;

public static class AuthorizationResultExtensions
{
    public static IActionResult ToActionResult(
        this ControllerBase controller,
        AuthorizationDecision decision)
    {
        if (decision.Allowed)
        {
            return controller.Ok();
        }

        return new ObjectResult(new ProblemDetails
        {
            Status = decision.StatusCode,
            Title = decision.ErrorCode == "unauthorized"
                ? "Unauthorized"
                : "Forbidden",
            Detail = decision.Detail,
            Type = $"https://httpstatuses.com/{decision.StatusCode}"
        })
        {
            StatusCode = decision.StatusCode
        };
    }
}
