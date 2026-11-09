using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Product.Api.UnitTest.TestData;
using Product.Application.Common.Abstractions;
using Product.Application.Common.Exceptions;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.Commands.DeleteProduct;

namespace Product.Api.UnitTest.Handlers.Commands;

public sealed class DeleteProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly DeleteProductCommandHandler _handler;

    public DeleteProductCommandHandlerTests() =>
        _handler = new DeleteProductCommandHandler(
            _repository.Object,
            _unitOfWork.Object,
            TestProducts.CreateTimeProvider(),
            NullLogger<DeleteProductCommandHandler>.Instance);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ExistingProduct_IsSoftDeleted()
    {
        var product = TestProducts.Hammer();
        _repository.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await _handler.Handle(new DeleteProductCommand(100001, "Admin"), Ct);

        Assert.True(result.IsSuccess);
        Assert.True(product.IsDeleted);
        Assert.Equal(TestProducts.Now, product.DeletedAt);
        Assert.Equal("Admin", product.DeletedBy);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MissingOrAlreadyDeletedProduct_ReturnsNotFound()
    {
        _repository.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync((ProductEntity?)null);

        var result = await _handler.Handle(new DeleteProductCommand(100001, "Admin"), Ct);

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConcurrentChange_ReturnsConflict()
    {
        _repository.Setup(r => r.GetByIdAsync(100001, It.IsAny<CancellationToken>())).ReturnsAsync(TestProducts.Hammer());
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict", new InvalidOperationException()));

        var result = await _handler.Handle(new DeleteProductCommand(100001, "Admin"), Ct);

        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }
}
