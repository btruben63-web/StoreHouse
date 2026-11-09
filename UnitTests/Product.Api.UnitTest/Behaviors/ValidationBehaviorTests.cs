using Product.Api.UnitTest.TestData;
using Product.Application.Common.Behaviors;
using Product.Application.Common.Results;
using Product.Application.Products.DTOs;
using Product.Application.Products.Queries.GetProductById;

namespace Product.Api.UnitTest.Behaviors;

public sealed class ValidationBehaviorTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task WithoutValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<GetProductByIdQuery, Result<ProductDTO>>([]);
        var nextCalled = false;

        var result = await behavior.Handle(new GetProductByIdQuery(1), _ =>
        {
            nextCalled = true;
            return Task.FromResult<Result<ProductDTO>>(TestProducts.HammerDTO);
        }, Ct);

        Assert.True(nextCalled);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ValidRequest_CallsNext()
    {
        var behavior = new ValidationBehavior<GetProductByIdQuery, Result<ProductDTO>>([new GetProductByIdQueryValidator()]);
        var nextCalled = false;

        var result = await behavior.Handle(new GetProductByIdQuery(100001), _ =>
        {
            nextCalled = true;
            return Task.FromResult<Result<ProductDTO>>(TestProducts.HammerDTO);
        }, Ct);

        Assert.True(nextCalled);
        Assert.Equal(TestProducts.HammerDTO, result.Value);
    }

    [Fact]
    public async Task InvalidRequest_ReturnsValidationFailureWithoutCallingNext()
    {
        var behavior = new ValidationBehavior<GetProductByIdQuery, Result<ProductDTO>>([new GetProductByIdQueryValidator()]);
        var nextCalled = false;

        var result = await behavior.Handle(new GetProductByIdQuery(1), _ =>
        {
            nextCalled = true;
            return Task.FromResult<Result<ProductDTO>>(TestProducts.HammerDTO);
        }, Ct);

        Assert.False(nextCalled);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.True(result.Error.ValidationErrors.ContainsKey("Id"));
    }
}
