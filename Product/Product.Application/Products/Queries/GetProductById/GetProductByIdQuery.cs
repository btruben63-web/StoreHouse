using MediatR;
using Product.Application.Common.Results;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(int Id) : IRequest<Result<ProductDTO>>;
