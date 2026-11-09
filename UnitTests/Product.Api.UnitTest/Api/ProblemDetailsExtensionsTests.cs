using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Product.Api.Extensions;

namespace Product.Api.UnitTest.Api;

public sealed class ProblemDetailsExtensionsTests
{
    [Theory]
    [InlineData(StatusCodes.Status400BadRequest, "Request.Invalid")]
    [InlineData(StatusCodes.Status401Unauthorized, "Auth.Unauthorized")]
    [InlineData(StatusCodes.Status403Forbidden, "Auth.Forbidden")]
    [InlineData(StatusCodes.Status404NotFound, "Route.NotFound")]
    [InlineData(StatusCodes.Status405MethodNotAllowed, "Request.MethodNotAllowed")]
    [InlineData(StatusCodes.Status500InternalServerError, "Server.Error")]
    [InlineData(StatusCodes.Status415UnsupportedMediaType, "Http.415")]
    [InlineData(StatusCodes.Status503ServiceUnavailable, "Http.503")]
    public void GetCodeForStatus_MapsStatusToCode(int status, string expectedCode) =>
        Assert.Equal(expectedCode, ProblemDetailsExtensions.GetCodeForStatus(status));

    [Fact]
    public void GetTraceId_WithCurrentActivity_ReturnsActivityId()
    {
        using var activity = new Activity("test").Start();
        var httpContext = new DefaultHttpContext { TraceIdentifier = "http-trace" };

        Assert.Equal(activity.Id, ProblemDetailsExtensions.GetTraceId(httpContext));
    }

    [Fact]
    public void GetTraceId_WithoutActivity_ReturnsHttpContextTraceIdentifier()
    {
        var previous = Activity.Current;
        Activity.Current = null;
        try
        {
            var httpContext = new DefaultHttpContext { TraceIdentifier = "http-trace" };

            Assert.Equal("http-trace", ProblemDetailsExtensions.GetTraceId(httpContext));
        }
        finally
        {
            Activity.Current = previous;
        }
    }

    [Fact]
    public void Customize_AddsCodeFromStatusAndTraceId()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        var context = new ProblemDetailsContext { HttpContext = httpContext, ProblemDetails = { Status = StatusCodes.Status401Unauthorized } };

        ProblemDetailsExtensions.Customize(context);

        Assert.Equal("Auth.Unauthorized", context.ProblemDetails.Extensions["code"]);
        Assert.NotNull(context.ProblemDetails.Extensions["traceId"]);
    }

    [Fact]
    public void Customize_WithoutProblemStatus_UsesResponseStatus()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
        var context = new ProblemDetailsContext { HttpContext = httpContext, ProblemDetails = new ProblemDetails() };

        ProblemDetailsExtensions.Customize(context);

        Assert.Equal("Auth.Forbidden", context.ProblemDetails.Extensions["code"]);
    }

    [Fact]
    public void Customize_DoesNotOverwriteExistingCodeAndTraceId()
    {
        var httpContext = new DefaultHttpContext();
        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = { Status = StatusCodes.Status404NotFound, Extensions = { ["code"] = "Product.NotFound", ["traceId"] = "existing" } }
        };

        ProblemDetailsExtensions.Customize(context);

        Assert.Equal("Product.NotFound", context.ProblemDetails.Extensions["code"]);
        Assert.Equal("existing", context.ProblemDetails.Extensions["traceId"]);
    }
}
