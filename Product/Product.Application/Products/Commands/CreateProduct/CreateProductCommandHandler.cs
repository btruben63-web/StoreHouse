using MapsterMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Product.Application.Common.Abstractions;
using Product.Application.Common.Logging;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.DTOs;
using Product.Domain.Entities;
using Product.Domain.Enums;

namespace Product.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    TimeProvider timeProvider,
    ILogger<CreateProductCommandHandler> logger)
    : IRequestHandler<CreateProductCommand, Result<ProductDTO>>
{
    public async Task<Result<ProductDTO>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().DateTime;
        var data = request.Product;

        var product = new ProductEntity
        {
            Name = data.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim(),
            Price = data.Price,
            CreatedAt = now,
            Stock = new Stock { Quantity = data.InitialStock, UpdatedAt = now }
        };

        if (data.InitialStock > 0)
        {
            product.StockMovements.Add(new StockMovement
            {
                Type = StockMovementType.Initial,
                Quantity = data.InitialStock,
                QuantityAfter = data.InitialStock,
                CreatedAt = now,
                CreatedBy = request.UserName
            });
        }

        repository.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.ProductCreated(product.Id, data.InitialStock);
        return mapper.Map<ProductDTO>(product);
    }
}
