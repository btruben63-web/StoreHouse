using MediatR;
using Product.Application.Common.DTOs;
using Product.Application.Common.Results;
using Product.Application.Products.Abstractions;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Queries.SearchProductsByName;

public sealed class SearchProductsByNameQueryHandler(IProductQueries queries)
    : IRequestHandler<SearchProductsByNameQuery, Result<PagedResultDTO<ProductDTO>>>
{
    public async Task<Result<PagedResultDTO<ProductDTO>>> Handle(SearchProductsByNameQuery request, CancellationToken cancellationToken) =>
        await queries.GetPageAsync(new ProductFilter(NameContains: request.Name.Trim()), request.LastItemId, request.PageSize, cancellationToken);
}
