using MediatR;
using Product.Application.Common.DTOs;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Queries.GetProducts;

public sealed class GetProductsQueryHandler(IProductQueries queries)
    : IRequestHandler<GetProductsQuery, Result<PagedResultDTO<ProductDTO>>>
{
    public async Task<Result<PagedResultDTO<ProductDTO>>> Handle(GetProductsQuery request, CancellationToken cancellationToken) =>
        await queries.GetPageAsync(ProductFilter.None, request.LastItemId, request.PageSize, cancellationToken);
}
