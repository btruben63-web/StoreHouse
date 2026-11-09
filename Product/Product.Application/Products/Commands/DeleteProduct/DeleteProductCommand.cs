using MediatR;
using Product.Application.Common.Results;

namespace Product.Application.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(int Id, string? UserName) : IRequest<Result>;
