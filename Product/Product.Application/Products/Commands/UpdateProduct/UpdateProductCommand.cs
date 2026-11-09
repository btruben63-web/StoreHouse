using MediatR;
using Product.Application.Common.Results;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(int Id, UpdateProductDTO Product) : IRequest<Result<ProductDTO>>;
