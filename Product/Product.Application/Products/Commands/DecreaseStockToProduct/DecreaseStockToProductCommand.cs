using MediatR;
using Product.Application.Common.Results;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Commands.DecreaseStockToProduct;

public sealed record DecreaseStockToProductCommand(int Id, int Quantity, string? UserName) : IRequest<Result<StockDTO>>;
