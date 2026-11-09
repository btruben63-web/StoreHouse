using MediatR;
using Microsoft.Extensions.Logging;
using Product.Application.Common.Abstractions;
using Product.Application.Common.Exceptions;
using Product.Application.Common.Logging;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;

namespace Product.Application.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<DeleteProductCommandHandler> logger)
    : IRequestHandler<DeleteProductCommand, Result>
{
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound(request.Id);
        }

        product.IsDeleted = true;
        product.DeletedAt = timeProvider.GetUtcNow().DateTime;
        product.DeletedBy = request.UserName;

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            logger.ProductConcurrencyConflict(request.Id);
            return ProductErrors.ConcurrencyConflict(request.Id);
        }

        logger.ProductDeleted(request.Id, request.UserName);
        return Result.Success();
    }
}
