using FluentValidation.TestHelper;
using Product.Application.Products.Queries.GetProductById;
using Product.Application.Products.Queries.GetProducts;
using Product.Application.Products.Queries.SearchProductsByName;
using Product.Application.Products.Queries.SearchProductsByStockRange;

namespace Product.Api.UnitTest.Validators;

public sealed class QueryValidatorsTests
{
    private readonly GetProductsQueryValidator _getProductsValidator = new();
    private readonly GetProductByIdQueryValidator _getByIdValidator = new();
    private readonly SearchProductsByNameQueryValidator _searchByNameValidator = new();
    private readonly SearchProductsByStockRangeQueryValidator _stockRangeValidator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void GetProducts_InvalidPageSize_Fails(int pageSize) =>
        _getProductsValidator.TestValidate(new GetProductsQuery(null, pageSize)).ShouldHaveValidationErrorFor(x => x.PageSize);

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public void GetProducts_ValidPageSize_Passes(int pageSize) =>
        _getProductsValidator.TestValidate(new GetProductsQuery(null, pageSize)).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void GetProducts_InvalidCursor_Fails() =>
        _getProductsValidator.TestValidate(new GetProductsQuery(99_999, 5)).ShouldHaveValidationErrorFor(x => x.LastItemId);

    [Fact]
    public void GetProducts_ValidCursor_Passes() =>
        _getProductsValidator.TestValidate(new GetProductsQuery(100_005, 5)).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void GetProductById_InvalidId_Fails() =>
        _getByIdValidator.TestValidate(new GetProductByIdQuery(99_999)).ShouldHaveValidationErrorFor(x => x.Id);

    [Fact]
    public void GetProductById_ValidId_Passes() =>
        _getByIdValidator.TestValidate(new GetProductByIdQuery(100_001)).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData(" a ")]
    public void SearchByName_TooShort_Fails(string name) =>
        _searchByNameValidator.TestValidate(new SearchProductsByNameQuery(name, null, 5)).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void SearchByName_TooLong_Fails() =>
        _searchByNameValidator.TestValidate(new SearchProductsByNameQuery(new string('a', 101), null, 5))
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void SearchByName_Valid_Passes() =>
        _searchByNameValidator.TestValidate(new SearchProductsByNameQuery("ma", null, 5)).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void SearchByName_InvalidPageSize_Fails() =>
        _searchByNameValidator.TestValidate(new SearchProductsByNameQuery("martelo", null, 0))
            .ShouldHaveValidationErrorFor(x => x.PageSize);

    [Fact]
    public void StockRange_NegativeMin_Fails() =>
        _stockRangeValidator.TestValidate(new SearchProductsByStockRangeQuery(-1, 10, null, 5))
            .ShouldHaveValidationErrorFor(x => x.Min);

    [Fact]
    public void StockRange_MaxBelowMin_Fails() =>
        _stockRangeValidator.TestValidate(new SearchProductsByStockRangeQuery(20, 10, null, 5))
            .ShouldHaveValidationErrorFor(x => x.Max);

    [Fact]
    public void StockRange_MinEqualToMax_Passes() =>
        _stockRangeValidator.TestValidate(new SearchProductsByStockRangeQuery(10, 10, null, 5))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void StockRange_InvalidCursor_Fails() =>
        _stockRangeValidator.TestValidate(new SearchProductsByStockRangeQuery(0, 10, 5, 5))
            .ShouldHaveValidationErrorFor(x => x.LastItemId);
}
