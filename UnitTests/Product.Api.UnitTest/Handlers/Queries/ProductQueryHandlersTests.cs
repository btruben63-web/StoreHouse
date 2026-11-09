using Moq;
using Product.Api.UnitTest.TestData;
using Product.Application.Common.DTOs;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.DTOs;
using Product.Application.Products.Queries.GetProductById;
using Product.Application.Products.Queries.GetProducts;
using Product.Application.Products.Queries.SearchProductsByName;
using Product.Application.Products.Queries.SearchProductsByStockRange;

namespace Product.Api.UnitTest.Handlers.Queries;

public sealed class ProductQueryHandlersTests
{
    private static readonly PagedResultDTO<ProductDTO> Page = new([TestProducts.HammerDTO], 100001, true);

    private readonly Mock<IProductQueries> _queries = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetProducts_UsesNoFilterAndForwardsPaging()
    {
        _queries.Setup(q => q.GetPageAsync(ProductFilter.None, 100005, 5, It.IsAny<CancellationToken>())).ReturnsAsync(Page);

        var result = await new GetProductsQueryHandler(_queries.Object).Handle(new GetProductsQuery(100005, 5), Ct);

        Assert.True(result.IsSuccess);
        Assert.Same(Page, result.Value);
    }

    [Fact]
    public async Task GetProductById_Found_ReturnsProduct()
    {
        _queries.Setup(q => q.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(TestProducts.HammerDTO);

        var result = await new GetProductByIdQueryHandler(_queries.Object).Handle(new GetProductByIdQuery(100001), Ct);

        Assert.Equal(TestProducts.HammerDTO, result.Value);
    }

    [Fact]
    public async Task GetProductById_Missing_ReturnsNotFound()
    {
        _queries.Setup(q => q.GetByIdAsync(100999, It.IsAny<CancellationToken>())).ReturnsAsync((ProductDTO?)null);

        var result = await new GetProductByIdQueryHandler(_queries.Object).Handle(new GetProductByIdQuery(100999), Ct);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
        Assert.Equal("Product.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task SearchProductsByName_TrimsNameIntoFilter()
    {
        _queries.Setup(q => q.GetPageAsync(new ProductFilter("mart", null, null), null, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Page);

        var result = await new SearchProductsByNameQueryHandler(_queries.Object)
            .Handle(new SearchProductsByNameQuery("  mart ", null, 5), Ct);

        Assert.Same(Page, result.Value);
    }

    [Fact]
    public async Task SearchProductsByStockRange_UsesMinAndMaxInFilter()
    {
        _queries.Setup(q => q.GetPageAsync(new ProductFilter(null, 10, 30), 100002, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Page);

        var result = await new SearchProductsByStockRangeQueryHandler(_queries.Object)
            .Handle(new SearchProductsByStockRangeQuery(10, 30, 100002, 5), Ct);

        Assert.Same(Page, result.Value);
    }
}
