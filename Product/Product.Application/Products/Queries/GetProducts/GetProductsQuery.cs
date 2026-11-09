using MediatR;
using Product.Application.Common.DTOs;
using Product.Application.Common.Results;
using Product.Application.Products.DTOs;
using Product.Domain.Constants;

namespace Product.Application.Products.Queries.GetProducts;

public sealed record GetProductsQuery(int? LastItemId, int PageSize = ProductConstraints.DefaultPageSize)
    : IRequest<Result<PagedResultDTO<ProductDTO>>>;
