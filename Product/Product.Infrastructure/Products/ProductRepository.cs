using Microsoft.EntityFrameworkCore;
using Product.Application.Products.Abstractions;
using Product.Domain.Entities;
using Product.Infrastructure.Persistence;

namespace Product.Infrastructure.Products;

internal sealed class ProductRepository(ProductDbContext context) : IProductRepository
{
    public Task<ProductEntity?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        context.Products
            .Include(p => p.Stock)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public void Add(ProductEntity product) => context.Products.Add(product);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
        context.Products.AnyAsync(p => p.Id == id, cancellationToken);

    public Task<int> AddStockAsync(int id, int quantity, DateTime now, CancellationToken cancellationToken)
    {
        var maxCurrentQuantity = int.MaxValue - quantity;
        return context.Stocks
            .Where(s => s.ProductId == id && s.Quantity <= maxCurrentQuantity)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.Quantity, s => s.Quantity + quantity)
                .SetProperty(s => s.UpdatedAt, now), cancellationToken);
    }

    public Task<int> DecreaseStockAsync(int id, int quantity, DateTime now, CancellationToken cancellationToken) =>
        context.Stocks
            .Where(s => s.ProductId == id && s.Quantity >= quantity)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.Quantity, s => s.Quantity - quantity)
                .SetProperty(s => s.UpdatedAt, now), cancellationToken);

    public Task<int> GetStockQuantityAsync(int id, CancellationToken cancellationToken) =>
        context.Stocks
            .Where(s => s.ProductId == id)
            .Select(s => s.Quantity)
            .FirstAsync(cancellationToken);

    public void AddMovement(StockMovement movement) => context.StockMovements.Add(movement);
}
