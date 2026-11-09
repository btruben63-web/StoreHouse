using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Product.Api.ErrorHandling;

namespace Product.Api.UnitTest.Api;

public sealed class GlobalExceptionHandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("Production", false)]
    [InlineData("Development", true)]
    public async Task UnhandledException_Writes500ProblemDetails(string environmentName, bool expectDetail)
    {
        var problemDetailsService = new CapturingProblemDetailsService();
        var environment = new Mock<IHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns(environmentName);
        var handler = new GlobalExceptionHandler(problemDetailsService, environment.Object, NullLogger<GlobalExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(httpContext, new InvalidOperationException("boom"), Ct);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        Assert.NotNull(problemDetailsService.Written);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemDetailsService.Written!.ProblemDetails.Status);
        Assert.Equal(expectDetail, problemDetailsService.Written.ProblemDetails.Detail is not null);
        Assert.Equal("Server.Error", problemDetailsService.Written.ProblemDetails.Extensions["code"]);
    }

    private sealed class CapturingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetailsContext? Written { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Written = context;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            Written = context;
            return ValueTask.FromResult(true);
        }
    }
}
