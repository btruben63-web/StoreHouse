using MediatR;
using Product.Application.Common.Results;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Commands.AddStockToProduct;

public sealed record AddStockToProductCommand(int Id, int Quantity, string? UserName) : IRequest<Result<StockDTO>>;
