using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Product.Domain.Entities;
using Product.Infrastructure.Persistence.Seed;

namespace Product.Infrastructure.Persistence.Configurations;

internal sealed class StockConfiguration : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        builder.ToTable("Stock", table => table.HasCheckConstraint("CK_Stock_Quantity", "\"Quantity\" >= 0"));

        builder.HasKey(s => s.ProductId);
        builder.Property(s => s.ProductId).ValueGeneratedNever();
        builder.Property(s => s.Version).IsRowVersion();

        builder.HasQueryFilter(s => !s.Product!.IsDeleted);
        builder.HasIndex(s => s.Quantity).HasDatabaseName("IX_Stock_Quantity");

        builder.HasData(ProductSeedData.Stocks);
    }
}
