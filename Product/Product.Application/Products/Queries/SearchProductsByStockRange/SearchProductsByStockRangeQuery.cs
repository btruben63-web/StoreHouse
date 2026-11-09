using MediatR;
using Product.Application.Common.DTOs;
using Product.Application.Common.Results;
using Product.Application.Products.DTOs;
using Product.Domain.Constants;

namespace Product.Application.Products.Queries.SearchProductsByStockRange;

public sealed record SearchProductsByStockRangeQuery(int Min, int Max, int? LastItemId, int PageSize = ProductConstraints.DefaultPageSize)
    : IRequest<Result<PagedResultDTO<ProductDTO>>>;
