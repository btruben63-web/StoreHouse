using System.Diagnostics;

namespace Product.Api.Extensions;

public static class ProblemDetailsExtensions
{
    public static string GetCodeForStatus(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "Request.Invalid",
        StatusCodes.Status401Unauthorized => "Auth.Unauthorized",
        StatusCodes.Status403Forbidden => "Auth.Forbidden",
        StatusCodes.Status404NotFound => "Route.NotFound",
        StatusCodes.Status405MethodNotAllowed => "Request.MethodNotAllowed",
        StatusCodes.Status500InternalServerError => "Server.Error",
        _ => $"Http.{status}"
    };

    public static string? GetTraceId(HttpContext? httpContext) =>
        Activity.Current?.Id ?? httpContext?.TraceIdentifier;

    public static void Customize(ProblemDetailsContext context)
    {
        var status = context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode;
        context.ProblemDetails.Extensions.TryAdd("code", GetCodeForStatus(status));
        context.ProblemDetails.Extensions.TryAdd("traceId", GetTraceId(context.HttpContext));
    }
}
