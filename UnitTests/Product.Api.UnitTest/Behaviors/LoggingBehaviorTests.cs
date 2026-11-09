using Microsoft.Extensions.Logging.Abstractions;
using Product.Api.UnitTest.TestData;
using Product.Application.Common.Behaviors;
using Product.Application.Common.Results;
using Product.Application.Products.DTOs;
using Product.Application.Products.Queries.GetProductById;

namespace Product.Api.UnitTest.Behaviors;

public sealed class LoggingBehaviorTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static LoggingBehavior<GetProductByIdQuery, Result<ProductDTO>> CreateBehavior() =>
        new(NullLogger<LoggingBehavior<GetProductByIdQuery, Result<ProductDTO>>>.Instance);

    [Fact]
    public async Task SuccessfulResponse_IsReturnedUnchanged()
    {
        Result<ProductDTO> expected = TestProducts.HammerDTO;

        var result = await CreateBehavior().Handle(new GetProductByIdQuery(100001), _ => Task.FromResult(expected), Ct);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task FailedResponse_IsReturnedUnchanged()
    {
        Result<ProductDTO> expected = new Error("Product.NotFound", "Not found.", ErrorType.NotFound);

        var result = await CreateBehavior().Handle(new GetProductByIdQuery(100001), _ => Task.FromResult(expected), Ct);

        Assert.Same(expected, result);
    }
}
