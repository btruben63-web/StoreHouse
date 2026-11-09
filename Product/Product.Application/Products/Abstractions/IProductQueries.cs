using Product.Application.Common.DTOs;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Abstractions;

public interface IProductQueries
{
    Task<ProductDTO?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<PagedResultDTO<ProductDTO>> GetPageAsync(
        ProductFilter filter,
        int? lastItemId,
        int pageSize,
        CancellationToken cancellationToken);
}
