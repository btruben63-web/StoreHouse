using Microsoft.EntityFrameworkCore;
using Product.Domain.Entities;
using Product.Infrastructure.Persistence.Configurations;

namespace Product.Infrastructure.Persistence;

public sealed class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    public DbSet<ProductEntity> Products => Set<ProductEntity>();

    public DbSet<Stock> Stocks => Set<Stock>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.HasSequence<int>(ProductConfiguration.IdSequenceName)
            .StartsAt(100_011)
            .IncrementsBy(1)
            .HasMin(100_000)
            .HasMax(999_999)
            .IsCyclic(false);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductDbContext).Assembly);

        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(entity => entity.GetProperties()))
        {
            if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
            {
                property.SetColumnType("timestamp without time zone");
            }
        }
    }
}
