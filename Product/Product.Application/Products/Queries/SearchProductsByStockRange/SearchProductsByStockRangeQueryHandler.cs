using MediatR;
using Product.Application.Common.DTOs;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Queries.SearchProductsByStockRange;

public sealed class SearchProductsByStockRangeQueryHandler(IProductQueries queries)
    : IRequestHandler<SearchProductsByStockRangeQuery, Result<PagedResultDTO<ProductDTO>>>
{
    public async Task<Result<PagedResultDTO<ProductDTO>>> Handle(SearchProductsByStockRangeQuery request, CancellationToken cancellationToken) =>
        await queries.GetPageAsync(
            new ProductFilter(MinStock: request.Min, MaxStock: request.Max),
            request.LastItemId,
            request.PageSize,
            cancellationToken);
}
