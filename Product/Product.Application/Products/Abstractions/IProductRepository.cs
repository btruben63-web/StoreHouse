using Product.Domain.Entities;

namespace Product.Application.Products.Abstractions;

public interface IProductRepository
{
    Task<ProductEntity?> GetByIdAsync(int id, CancellationToken cancellationToken);

    void Add(ProductEntity product);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);

    Task<int> AddStockAsync(int id, int quantity, DateTime now, CancellationToken cancellationToken);

    Task<int> DecreaseStockAsync(int id, int quantity, DateTime now, CancellationToken cancellationToken);

    Task<int> GetStockQuantityAsync(int id, CancellationToken cancellationToken);

    void AddMovement(StockMovement movement);
}
