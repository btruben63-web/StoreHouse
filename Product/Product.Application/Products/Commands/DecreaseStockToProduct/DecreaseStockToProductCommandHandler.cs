using MediatR;
using Microsoft.Extensions.Logging;
using Product.Application.Common.Abstractions;
using Product.Application.Common.Logging;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.DTOs;
using Product.Domain.Entities;
using Product.Domain.Enums;

namespace Product.Application.Products.Commands.DecreaseStockToProduct;

public sealed class DecreaseStockToProductCommandHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<DecreaseStockToProductCommandHandler> logger)
    : IRequestHandler<DecreaseStockToProductCommand, Result<StockDTO>>
{
    public Task<Result<StockDTO>> Handle(DecreaseStockToProductCommand request, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteTransactionAsync<Result<StockDTO>>(async token =>
        {
            var now = timeProvider.GetUtcNow().DateTime;
            var affectedRows = await repository.DecreaseStockAsync(request.Id, request.Quantity, now, token);
            if (affectedRows == 0)
            {
                if (!await repository.ExistsAsync(request.Id, token))
                {
                    return ProductErrors.NotFound(request.Id);
                }

                var available = await repository.GetStockQuantityAsync(request.Id, token);
                logger.InsufficientStock(request.Id, request.Quantity, available);
                return ProductErrors.InsufficientStock(request.Id, available, request.Quantity);
            }

            var quantityAfter = await repository.GetStockQuantityAsync(request.Id, token);
            repository.AddMovement(new StockMovement
            {
                ProductId = request.Id,
                Type = StockMovementType.Decrease,
                Quantity = request.Quantity,
                QuantityAfter = quantityAfter,
                CreatedAt = now,
                CreatedBy = request.UserName
            });
            await unitOfWork.SaveChangesAsync(token);

            logger.StockDecreased(request.Id, request.Quantity, quantityAfter);
            return new StockDTO(request.Id, quantityAfter);
        }, cancellationToken);
}
