using Mapster;
using Microsoft.EntityFrameworkCore;
using Product.Application.Common.DTOs;
using Product.Application.Products.Abstractions;
using Product.Application.Products.DTOs;
using Product.Infrastructure.Persistence;

namespace Product.Infrastructure.Products;

internal sealed class ProductQueries(ProductDbContext context, TypeAdapterConfig mappingConfig) : IProductQueries
{
    public Task<ProductDTO?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        context.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .ProjectToType<ProductDTO>(mappingConfig)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResultDTO<ProductDTO>> GetPageAsync(
        ProductFilter filter,
        int? lastItemId,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<ProductEntity> query = context.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.NameContains))
        {
            var pattern = $"%{LikePatternEscaper.Escape(filter.NameContains)}%";
            query = query.Where(p => EF.Functions.ILike(p.Name, pattern, LikePatternEscaper.EscapeCharacter));
        }

        if (filter.MinStock is int minStock)
        {
            query = query.Where(p => p.Stock!.Quantity >= minStock);
        }

        if (filter.MaxStock is int maxStock)
        {
            query = query.Where(p => p.Stock!.Quantity <= maxStock);
        }

        if (lastItemId is int cursor)
        {
            query = query.Where(p => p.Id > cursor);
        }

        var items = await query
            .OrderBy(p => p.Id)
            .Take(pageSize + 1)
            .ProjectToType<ProductDTO>(mappingConfig)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > pageSize;
        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        return new PagedResultDTO<ProductDTO>(items, hasMore ? items[^1].Id : null, hasMore);
    }
}
