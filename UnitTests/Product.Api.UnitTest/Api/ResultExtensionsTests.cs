using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Moq;
using Product.Api.Controllers;
using Product.Api.Extensions;
using Product.Application.Common.Results;

namespace Product.Api.UnitTest.Api;

public sealed class ResultExtensionsTests
{
    private static ProductsController CreateController() =>
        new(Mock.Of<ISender>()) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

    [Theory]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.BusinessRule, StatusCodes.Status422UnprocessableEntity)]
    public void Error_IsConvertedToProblemDetailsWithMatchingStatus(ErrorType type, int expectedStatus)
    {
        var response = CreateController().ToProblem(new Error("Product.Code", "Something happened.", type));

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(expectedStatus, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(expectedStatus, problem.Status);
        Assert.Equal("Something happened.", problem.Detail);
        Assert.Equal("Product.Code", problem.Extensions["code"]);
        Assert.True(problem.Extensions.ContainsKey("traceId"));
    }

    [Fact]
    public void ValidationError_IsConvertedToValidationProblemDetails()
    {
        var error = Error.Validation(new Dictionary<string, string[]> { ["Name"] = ["'Name' must not be empty."] });

        var response = CreateController().ToProblem(error);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var problem = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.Equal(["'Name' must not be empty."], problem.Errors["Name"]);
    }

    [Fact]
    public void InvalidModelState_IsConvertedToValidationProblemDetailsWithCode()
    {
        var context = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        context.ModelState.AddModelError("name", "The name field is required.");

        var response = ResultExtensions.InvalidModelStateResponse(context);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var problem = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.Equal(["The name field is required."], problem.Errors["name"]);
        Assert.Equal("Validation.Failed", problem.Extensions["code"]);
        Assert.NotNull(problem.Extensions["traceId"]);
        Assert.Equal(["application/problem+json"], objectResult.ContentTypes);
    }

    [Fact]
    public void ProductsController_DoesNotForceResponseContentTypeAtClassLevel() =>
        Assert.Empty(typeof(ProductsController).GetCustomAttributes(typeof(ProducesAttribute), inherit: true));
}
