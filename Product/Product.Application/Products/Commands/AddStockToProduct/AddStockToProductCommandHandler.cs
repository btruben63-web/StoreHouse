using MediatR;
using Microsoft.Extensions.Logging;
using Product.Application.Common.Abstractions;
using Product.Application.Common.Logging;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.DTOs;
using Product.Domain.Entities;
using Product.Domain.Enums;

namespace Product.Application.Products.Commands.AddStockToProduct;

public sealed class AddStockToProductCommandHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<AddStockToProductCommandHandler> logger)
    : IRequestHandler<AddStockToProductCommand, Result<StockDTO>>
{
    public Task<Result<StockDTO>> Handle(AddStockToProductCommand request, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteTransactionAsync<Result<StockDTO>>(async token =>
        {
            var now = timeProvider.GetUtcNow().DateTime;
            var affectedRows = await repository.AddStockAsync(request.Id, request.Quantity, now, token);
            if (affectedRows == 0)
            {
                if (!await repository.ExistsAsync(request.Id, token))
                {
                    return ProductErrors.NotFound(request.Id);
                }

                logger.StockLimitExceeded(request.Id, request.Quantity);
                return ProductErrors.StockLimitExceeded(request.Id);
            }

            var quantityAfter = await repository.GetStockQuantityAsync(request.Id, token);
            repository.AddMovement(new StockMovement
            {
                ProductId = request.Id,
                Type = StockMovementType.Increase,
                Quantity = request.Quantity,
                QuantityAfter = quantityAfter,
                CreatedAt = now,
                CreatedBy = request.UserName
            });
            await unitOfWork.SaveChangesAsync(token);

            logger.StockIncreased(request.Id, request.Quantity, quantityAfter);
            return new StockDTO(request.Id, quantityAfter);
        }, cancellationToken);
}
