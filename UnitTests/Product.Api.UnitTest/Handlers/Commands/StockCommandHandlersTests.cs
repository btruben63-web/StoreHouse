using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Product.Api.UnitTest.TestData;
using Product.Application.Common.Abstractions;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.Commands.AddStockToProduct;
using Product.Application.Products.Commands.DecreaseStockToProduct;
using Product.Application.Products.DTOs;
using Product.Domain.Entities;
using Product.Domain.Enums;

namespace Product.Api.UnitTest.Handlers.Commands;

public sealed class StockCommandHandlersTests
{
    private const int ProductId = 100001;

    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FakeTimeProvider _timeProvider = TestProducts.CreateTimeProvider();
    private StockMovement? _addedMovement;

    public StockCommandHandlersTests()
    {
        _unitOfWork
            .Setup(u => u.ExecuteTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<Result<StockDTO>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<Result<StockDTO>>> operation, CancellationToken token) => operation(token));
        _repository.Setup(r => r.AddMovement(It.IsAny<StockMovement>())).Callback<StockMovement>(m => _addedMovement = m);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private AddStockToProductCommandHandler AddHandler() =>
        new(_repository.Object, _unitOfWork.Object, _timeProvider, NullLogger<AddStockToProductCommandHandler>.Instance);

    private DecreaseStockToProductCommandHandler DecreaseHandler() =>
        new(_repository.Object, _unitOfWork.Object, _timeProvider, NullLogger<DecreaseStockToProductCommandHandler>.Instance);

    [Fact]
    public async Task AddStock_UpdatesStockRecordsMovementAndRunsInTransaction()
    {
        _repository.Setup(r => r.AddStockAsync(ProductId, 10, TestProducts.Now, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _repository.Setup(r => r.GetStockQuantityAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(50);

        var result = await AddHandler().Handle(new AddStockToProductCommand(ProductId, 10, "Manager"), Ct);

        Assert.Equal(new StockDTO(ProductId, 50), result.Value);
        Assert.NotNull(_addedMovement);
        Assert.Equal(StockMovementType.Increase, _addedMovement!.Type);
        Assert.Equal(10, _addedMovement.Quantity);
        Assert.Equal(50, _addedMovement.QuantityAfter);
        Assert.Equal("Manager", _addedMovement.CreatedBy);
        Assert.Equal(TestProducts.Now, _addedMovement.CreatedAt);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.ExecuteTransactionAsync(
            It.IsAny<Func<CancellationToken, Task<Result<StockDTO>>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddStock_UnknownProduct_ReturnsNotFound()
    {
        _repository.Setup(r => r.AddStockAsync(ProductId, 10, TestProducts.Now, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _repository.Setup(r => r.ExistsAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await AddHandler().Handle(new AddStockToProductCommand(ProductId, 10, "Manager"), Ct);

        Assert.Equal("Product.NotFound", result.Error!.Code);
        Assert.Null(_addedMovement);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddStock_ExistingProductNotUpdated_ReturnsStockLimitExceeded()
    {
        _repository.Setup(r => r.AddStockAsync(ProductId, 10, TestProducts.Now, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _repository.Setup(r => r.ExistsAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await AddHandler().Handle(new AddStockToProductCommand(ProductId, 10, "Manager"), Ct);

        Assert.Equal(ErrorType.BusinessRule, result.Error!.Type);
        Assert.Equal("Product.StockLimitExceeded", result.Error.Code);
    }

    [Fact]
    public async Task DecreaseStock_UpdatesStockAndRecordsMovement()
    {
        _repository.Setup(r => r.DecreaseStockAsync(ProductId, 5, TestProducts.Now, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _repository.Setup(r => r.GetStockQuantityAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(35);

        var result = await DecreaseHandler().Handle(new DecreaseStockToProductCommand(ProductId, 5, "Manager"), Ct);

        Assert.Equal(new StockDTO(ProductId, 35), result.Value);
        Assert.Equal(StockMovementType.Decrease, _addedMovement!.Type);
        Assert.Equal(5, _addedMovement.Quantity);
        Assert.Equal(35, _addedMovement.QuantityAfter);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DecreaseStock_NotEnoughStock_ReturnsInsufficientStock()
    {
        _repository.Setup(r => r.DecreaseStockAsync(ProductId, 50, TestProducts.Now, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _repository.Setup(r => r.ExistsAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(r => r.GetStockQuantityAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(3);

        var result = await DecreaseHandler().Handle(new DecreaseStockToProductCommand(ProductId, 50, "Manager"), Ct);

        Assert.Equal(ErrorType.BusinessRule, result.Error!.Type);
        Assert.Equal("Product.InsufficientStock", result.Error.Code);
        Assert.Contains("3", result.Error.Message);
        Assert.Null(_addedMovement);
    }

    [Fact]
    public async Task DecreaseStock_UnknownProduct_ReturnsNotFound()
    {
        _repository.Setup(r => r.DecreaseStockAsync(ProductId, 5, TestProducts.Now, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _repository.Setup(r => r.ExistsAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await DecreaseHandler().Handle(new DecreaseStockToProductCommand(ProductId, 5, "Manager"), Ct);

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }
}
