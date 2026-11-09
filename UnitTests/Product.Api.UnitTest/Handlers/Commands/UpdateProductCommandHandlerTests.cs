using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Product.Api.UnitTest.TestData;
using Product.Application.Common.Abstractions;
using Product.Application.Common.Exceptions;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.Commands.UpdateProduct;
using Product.Application.Products.DTOs;

namespace Product.Api.UnitTest.Handlers.Commands;

public sealed class UpdateProductCommandHandlerTests
{
    private static readonly UpdateProductDTO Changes = new(" Martelo Pro ", "Cabo de fibra.", 15.50m);

    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly UpdateProductCommandHandler _handler;

    public UpdateProductCommandHandlerTests() =>
        _handler = new UpdateProductCommandHandler(
            _repository.Object,
            _unitOfWork.Object,
            TestMapping.CreateMapper(),
            TestProducts.CreateTimeProvider(),
            NullLogger<UpdateProductCommandHandler>.Instance);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task UnknownProduct_ReturnsNotFoundWithoutSaving()
    {
        _repository.Setup(r => r.GetByIdAsync(100999, It.IsAny<CancellationToken>())).ReturnsAsync((ProductEntity?)null);

        var result = await _handler.Handle(new UpdateProductCommand(100999, Changes), Ct);

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExistingProduct_UpdatesFieldsAndReturnsDTO()
    {
        var product = TestProducts.Hammer(stock: 40);
        _repository.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await _handler.Handle(new UpdateProductCommand(100001, Changes), Ct);

        Assert.Equal(new ProductDTO(100001, "Martelo Pro", "Cabo de fibra.", 15.50m, 40), result.Value);
        Assert.Equal(TestProducts.Now, product.UpdatedAt);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConcurrentChange_ReturnsConflict()
    {
        _repository.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(TestProducts.Hammer());
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict", new InvalidOperationException()));

        var result = await _handler.Handle(new UpdateProductCommand(100001, Changes), Ct);

        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
        Assert.Equal("Product.ConcurrencyConflict", result.Error.Code);
    }
}
