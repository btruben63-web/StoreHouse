using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Product.Application.Common.Results;

namespace Product.Api.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller, Func<T, IActionResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : controller.ToProblem(result.Error!);

    public static IActionResult ToActionResult(this Result result, ControllerBase controller, Func<IActionResult> onSuccess) =>
        result.IsSuccess ? onSuccess() : controller.ToProblem(result.Error!);

    public static IActionResult ToProblem(this ControllerBase controller, Error error) =>
        ToProblem(error, controller.HttpContext);

    public static IActionResult InvalidModelStateResponse(ActionContext context)
    {
        var errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors
                    .Select(modelError => string.IsNullOrEmpty(modelError.ErrorMessage) ? "The value is invalid." : modelError.ErrorMessage)
                    .ToArray());

        return ToProblem(Error.Validation(errors), context.HttpContext);
    }

    public static IActionResult ToProblem(Error error, HttpContext? httpContext)
    {
        var (status, type) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "https://tools.ietf.org/html/rfc9110#section-15.5.1"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "https://tools.ietf.org/html/rfc9110#section-15.5.10"),
            ErrorType.BusinessRule => (StatusCodes.Status422UnprocessableEntity, "https://tools.ietf.org/html/rfc9110#section-15.5.21"),
            _ => (StatusCodes.Status500InternalServerError, "https://tools.ietf.org/html/rfc9110#section-15.6.1")
        };

        var problem = error.Type == ErrorType.Validation
            ? new ValidationProblemDetails(error.ValidationErrors.ToDictionary(entry => entry.Key, entry => entry.Value))
            {
                Title = error.Message
            }
            : new ProblemDetails
            {
                Title = ReasonPhrases.GetReasonPhrase(status),
                Detail = error.Message
            };

        problem.Status = status;
        problem.Type = type;
        problem.Instance = httpContext?.Request.Path;
        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = ProblemDetailsExtensions.GetTraceId(httpContext);

        return new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
    }
}
