using MediatR;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryHandler(IProductQueries queries)
    : IRequestHandler<GetProductByIdQuery, Result<ProductDTO>>
{
    public async Task<Result<ProductDTO>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await queries.GetByIdAsync(request.Id, cancellationToken);
        return product is null ? ProductErrors.NotFound(request.Id) : product;
    }
}
