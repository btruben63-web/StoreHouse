using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Product.Domain.Constants;
using Product.Domain.Entities;
using Product.Infrastructure.Persistence.Seed;

namespace Product.Infrastructure.Persistence.Configurations;

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovement", table =>
        {
            table.HasCheckConstraint("CK_StockMovement_Type", "\"Type\" IN (1, 2, 3)");
            table.HasCheckConstraint("CK_StockMovement_Quantity", "\"Quantity\" > 0");
        });

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: 11);
        builder.Property(m => m.Type).HasConversion<short>();
        builder.Property(m => m.CreatedBy).HasMaxLength(ProductConstraints.UserNameMaxLength);

        builder.HasQueryFilter(m => !m.Product!.IsDeleted);
        builder.HasIndex(m => new { m.ProductId, m.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_StockMovement_ProductId_CreatedAt");

        builder.HasData(ProductSeedData.Movements);
    }
}
