using MapsterMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Product.Application.Common.Abstractions;
using Product.Application.Common.Exceptions;
using Product.Application.Common.Logging;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    TimeProvider timeProvider,
    ILogger<UpdateProductCommandHandler> logger)
    : IRequestHandler<UpdateProductCommand, Result<ProductDTO>>
{
    public async Task<Result<ProductDTO>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound(request.Id);
        }

        var data = request.Product;
        product.Name = data.Name.Trim();
        product.Description = string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim();
        product.Price = data.Price;
        product.UpdatedAt = timeProvider.GetUtcNow().DateTime;

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            logger.ProductConcurrencyConflict(request.Id);
            return ProductErrors.ConcurrencyConflict(request.Id);
        }

        logger.ProductUpdated(request.Id);
        return mapper.Map<ProductDTO>(product);
    }
}
