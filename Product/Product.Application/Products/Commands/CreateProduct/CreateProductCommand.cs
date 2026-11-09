using MediatR;
using Product.Application.Common.Results;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(CreateProductDTO Product, string? UserName) : IRequest<Result<ProductDTO>>;
