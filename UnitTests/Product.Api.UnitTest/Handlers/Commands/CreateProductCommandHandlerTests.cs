using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Product.Api.UnitTest.TestData;
using Product.Application.Common.Abstractions;
using Product.Application.Products.Abstractions;
using Product.Application.Products.Commands.CreateProduct;
using Product.Application.Products.DTOs;
using Product.Domain.Enums;

namespace Product.Api.UnitTest.Handlers.Commands;

public sealed class CreateProductCommandHandlerTests
{
    private const int GeneratedId = 100011;

    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CreateProductCommandHandler _handler;
    private ProductEntity? _addedProduct;

    public CreateProductCommandHandlerTests()
    {
        _repository.Setup(r => r.Add(It.IsAny<ProductEntity>())).Callback<ProductEntity>(product => _addedProduct = product);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => _addedProduct!.Id = GeneratedId)
            .ReturnsAsync(3);

        _handler = new CreateProductCommandHandler(
            _repository.Object,
            _unitOfWork.Object,
            TestMapping.CreateMapper(),
            TestProducts.CreateTimeProvider(),
            NullLogger<CreateProductCommandHandler>.Instance);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task WithInitialStock_CreatesProductStockAndInitialMovement()
    {
        var command = new CreateProductCommand(new CreateProductDTO("Grampos", "Caixa de 100", 3.50m, 10), "Admin");

        var result = await _handler.Handle(command, Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(new ProductDTO(GeneratedId, "Grampos", "Caixa de 100", 3.50m, 10), result.Value);
        Assert.NotNull(_addedProduct);
        Assert.Equal(TestProducts.Now, _addedProduct!.CreatedAt);
        Assert.Equal(10, _addedProduct.Stock!.Quantity);
        Assert.Equal(TestProducts.Now, _addedProduct.Stock.UpdatedAt);

        var movement = Assert.Single(_addedProduct.StockMovements);
        Assert.Equal(StockMovementType.Initial, movement.Type);
        Assert.Equal(10, movement.Quantity);
        Assert.Equal(10, movement.QuantityAfter);
        Assert.Equal("Admin", movement.CreatedBy);
        Assert.Equal(TestProducts.Now, movement.CreatedAt);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WithZeroInitialStock_CreatesStockWithoutMovement()
    {
        var command = new CreateProductCommand(new CreateProductDTO("Grampos", null, 3.50m, 0), "Admin");

        var result = await _handler.Handle(command, Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, _addedProduct!.Stock!.Quantity);
        Assert.Empty(_addedProduct.StockMovements);
    }

    [Fact]
    public async Task TrimsTextFieldsAndTurnsBlankDescriptionIntoNull()
    {
        var command = new CreateProductCommand(new CreateProductDTO("  Grampos  ", "   ", 3.50m, 1), "Admin");

        await _handler.Handle(command, Ct);

        Assert.Equal("Grampos", _addedProduct!.Name);
        Assert.Null(_addedProduct.Description);
    }
}
