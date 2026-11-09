using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using Product.Api.Controllers;
using Product.Api.UnitTest.TestData;
using Product.Application.Common.DTOs;
using Product.Application.Common.Results;
using Product.Application.Products;
using Product.Application.Products.Commands.AddStockToProduct;
using Product.Application.Products.Commands.CreateProduct;
using Product.Application.Products.Commands.DecreaseStockToProduct;
using Product.Application.Products.Commands.DeleteProduct;
using Product.Application.Products.Commands.UpdateProduct;
using Product.Application.Products.DTOs;
using Product.Application.Products.Queries.GetProductById;
using Product.Application.Products.Queries.GetProducts;
using Product.Application.Products.Queries.SearchProductsByName;
using Product.Application.Products.Queries.SearchProductsByStockRange;

namespace Product.Api.UnitTest.Api;

public sealed class ProductsControllerTests
{
    private static readonly PagedResultDTO<ProductDTO> Page = new([TestProducts.HammerDTO], null, false);

    private readonly Mock<ISender> _sender = new();
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("name", "Admin"), new Claim("role", "Admin")], "Test", "name", "role"));
        _controller = new ProductsController(_sender.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } }
        };
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static int? StatusOf(IActionResult response) => Assert.IsAssignableFrom<IStatusCodeActionResult>(response).StatusCode;

    [Fact]
    public async Task GetProducts_ReturnsOkWithPage()
    {
        _sender.Setup(s => s.Send(It.Is<GetProductsQuery>(q => q.LastItemId == null && q.PageSize == 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResultDTO<ProductDTO>>.Success(Page));

        var response = await _controller.GetProducts(null, 5, Ct);

        Assert.Same(Page, Assert.IsType<OkObjectResult>(response).Value);
    }

    [Fact]
    public async Task GetProducts_InvalidPaging_ReturnsBadRequest()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetProductsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResultDTO<ProductDTO>>.Failure(
                Error.Validation(new Dictionary<string, string[]> { ["PageSize"] = ["invalid"] })));

        var response = await _controller.GetProducts(null, 0, Ct);

        Assert.Equal(StatusCodes.Status400BadRequest, StatusOf(response));
    }

    [Fact]
    public async Task GetProductById_Found_ReturnsOk()
    {
        _sender.Setup(s => s.Send(It.Is<GetProductByIdQuery>(q => q.Id == 100001), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProductDTO>.Success(TestProducts.HammerDTO));

        var response = await _controller.GetProductById(100001, Ct);

        Assert.Equal(TestProducts.HammerDTO, Assert.IsType<OkObjectResult>(response).Value);
    }

    [Fact]
    public async Task GetProductById_Missing_ReturnsNotFound()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetProductByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProductDTO>.Failure(ProductErrors.NotFound(100999)));

        var response = await _controller.GetProductById(100999, Ct);

        Assert.Equal(StatusCodes.Status404NotFound, StatusOf(response));
    }

    [Fact]
    public async Task CreateProduct_ReturnsCreatedAtGetProductByIdAndPassesUserName()
    {
        var created = TestProducts.HammerDTO with { Id = 100011 };
        _sender.Setup(s => s.Send(It.IsAny<CreateProductCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProductDTO>.Success(created));

        var response = await _controller.CreateProduct(
            new CreateProductDTO("Martelo de Carpinteiro", null, 12.90m, 40), Ct);

        var createdResult = Assert.IsType<CreatedAtActionResult>(response);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal(nameof(ProductsController.GetProductById), createdResult.ActionName);
        Assert.Equal(100011, createdResult.RouteValues!["id"]);
        Assert.Same(created, createdResult.Value);
        _sender.Verify(s => s.Send(It.Is<CreateProductCommand>(c => c.UserName == "Admin"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProduct_ReturnsOk()
    {
        _sender.Setup(s => s.Send(It.Is<UpdateProductCommand>(c => c.Id == 100001), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProductDTO>.Success(TestProducts.HammerDTO));

        var response = await _controller.UpdateProduct(100001, new UpdateProductDTO("Martelo", null, 12.90m), Ct);

        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
    public async Task UpdateProduct_Conflict_ReturnsConflict()
    {
        _sender.Setup(s => s.Send(It.IsAny<UpdateProductCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProductDTO>.Failure(ProductErrors.ConcurrencyConflict(100001)));

        var response = await _controller.UpdateProduct(100001, new UpdateProductDTO("Martelo", null, 12.90m), Ct);

        Assert.Equal(StatusCodes.Status409Conflict, StatusOf(response));
    }

    [Fact]
    public async Task DeleteProduct_ReturnsNoContentAndPassesUserName()
    {
        _sender.Setup(s => s.Send(It.IsAny<DeleteProductCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        var response = await _controller.DeleteProduct(100001, Ct);

        Assert.IsType<NoContentResult>(response);
        _sender.Verify(s => s.Send(It.Is<DeleteProductCommand>(c => c.Id == 100001 && c.UserName == "Admin"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteProduct_Missing_ReturnsNotFound()
    {
        _sender.Setup(s => s.Send(It.IsAny<DeleteProductCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(ProductErrors.NotFound(100001)));

        var response = await _controller.DeleteProduct(100001, Ct);

        Assert.Equal(StatusCodes.Status404NotFound, StatusOf(response));
    }

    [Fact]
    public async Task DecreaseStockToProduct_ReturnsOkWithStock()
    {
        _sender.Setup(s => s.Send(It.Is<DecreaseStockToProductCommand>(c => c.Id == 100001 && c.Quantity == 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StockDTO>.Success(new StockDTO(100001, 35)));

        var response = await _controller.DecreaseStockToProduct(100001, 5, Ct);

        Assert.Equal(new StockDTO(100001, 35), Assert.IsType<OkObjectResult>(response).Value);
    }

    [Fact]
    public async Task DecreaseStockToProduct_InsufficientStock_ReturnsUnprocessableEntity()
    {
        _sender.Setup(s => s.Send(It.IsAny<DecreaseStockToProductCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StockDTO>.Failure(ProductErrors.InsufficientStock(100001, 3, 50)));

        var response = await _controller.DecreaseStockToProduct(100001, 50, Ct);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, StatusOf(response));
    }

    [Fact]
    public async Task AddStockToProduct_ReturnsOkWithStock()
    {
        _sender.Setup(s => s.Send(It.Is<AddStockToProductCommand>(c => c.Id == 100001 && c.Quantity == 10), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StockDTO>.Success(new StockDTO(100001, 50)));

        var response = await _controller.AddStockToProduct(100001, 10, Ct);

        Assert.Equal(new StockDTO(100001, 50), Assert.IsType<OkObjectResult>(response).Value);
    }

    [Fact]
    public async Task AddStockToProduct_Missing_ReturnsNotFound()
    {
        _sender.Setup(s => s.Send(It.IsAny<AddStockToProductCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StockDTO>.Failure(ProductErrors.NotFound(100999)));

        var response = await _controller.AddStockToProduct(100999, 10, Ct);

        Assert.Equal(StatusCodes.Status404NotFound, StatusOf(response));
    }

    [Fact]
    public async Task SearchProductsByName_ForwardsParametersAndReturnsOk()
    {
        _sender.Setup(s => s.Send(It.Is<SearchProductsByNameQuery>(q => q.Name == "mart" && q.LastItemId == null && q.PageSize == 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResultDTO<ProductDTO>>.Success(Page));

        var response = await _controller.SearchProductsByName("mart", null, 5, Ct);

        Assert.Same(Page, Assert.IsType<OkObjectResult>(response).Value);
    }

    [Fact]
    public async Task SearchProductsByStockRange_ForwardsParametersAndReturnsOk()
    {
        _sender.Setup(s => s.Send(It.Is<SearchProductsByStockRangeQuery>(q => q.Min == 0 && q.Max == 30), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResultDTO<ProductDTO>>.Success(Page));

        var response = await _controller.SearchProductsByStockRange(0, 30, null, 5, Ct);

        Assert.Same(Page, Assert.IsType<OkObjectResult>(response).Value);
    }
}
