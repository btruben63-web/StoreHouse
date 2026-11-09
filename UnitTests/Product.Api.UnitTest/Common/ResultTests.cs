using Product.Application.Common.Results;

namespace Product.Api.UnitTest.Common;

public sealed class ResultTests
{
    private static readonly Error NotFoundError = new("Product.NotFound", "Not found.", ErrorType.NotFound);

    [Fact]
    public void Success_IsSuccessWithoutError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_ExposesError()
    {
        var result = Result.Failure(NotFoundError);

        Assert.True(result.IsFailure);
        Assert.Same(NotFoundError, result.Error);
    }

    [Fact]
    public void Failure_WithNullError_Throws() =>
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));

    [Fact]
    public void GenericSuccess_ExposesValue()
    {
        Result<string> result = "value";

        Assert.True(result.IsSuccess);
        Assert.Equal("value", result.Value);
    }

    [Fact]
    public void GenericFailure_ThrowsWhenValueIsRead()
    {
        Result<string> result = NotFoundError;

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ValidationError_CarriesFieldErrors()
    {
        var error = Error.Validation(new Dictionary<string, string[]> { ["Name"] = ["'Name' must not be empty."] });

        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("Validation.Failed", error.Code);
        Assert.Equal(["'Name' must not be empty."], error.ValidationErrors["Name"]);
    }

    [Fact]
    public void StaticAbstractFailure_CreatesFailureOfTheRequestedType()
    {
        var generic = CreateFailure<Result<int>>(NotFoundError);
        var nonGeneric = CreateFailure<Result>(NotFoundError);

        Assert.True(generic.IsFailure);
        Assert.True(nonGeneric.IsFailure);
    }

    private static TResult CreateFailure<TResult>(Error error) where TResult : IResultBase<TResult> =>
        TResult.Failure(error);
}
