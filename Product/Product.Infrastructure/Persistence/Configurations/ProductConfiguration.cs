using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Product.Domain.Constants;
using Product.Domain.Entities;
using Product.Infrastructure.Persistence.Seed;

namespace Product.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<ProductEntity>
{
    public const string IdSequenceName = "ProductIdSequence";
    private const string NotDeletedFilter = "\"IsDeleted\" = false";

    public void Configure(EntityTypeBuilder<ProductEntity> builder)
    {
        builder.ToTable("Product", table => table.HasCheckConstraint("CK_Product_Price", "\"Price\" > 0"));

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).UseSequence(IdSequenceName);
        builder.Property(p => p.Name).HasMaxLength(ProductConstraints.NameMaxLength).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(ProductConstraints.DescriptionMaxLength);
        builder.Property(p => p.Price).HasPrecision(ProductConstraints.PricePrecision, ProductConstraints.PriceScale);
        builder.Property(p => p.IsDeleted).HasDefaultValue(false);
        builder.Property(p => p.DeletedBy).HasMaxLength(ProductConstraints.UserNameMaxLength);
        builder.Property(p => p.Version).IsRowVersion();

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => p.Name)
            .HasDatabaseName("IX_Product_Name_Trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops")
            .HasFilter(NotDeletedFilter);
        builder.HasOne(p => p.Stock)
            .WithOne(s => s.Product)
            .HasForeignKey<Stock>(s => s.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(p => p.StockMovements)
            .WithOne(m => m.Product)
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(ProductSeedData.Products);
    }
}
