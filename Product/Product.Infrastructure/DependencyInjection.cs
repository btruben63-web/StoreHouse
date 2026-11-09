using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Product.Application.Common.Abstractions;
using Product.Application.Products.Abstractions;
using Product.Infrastructure.Persistence;
using Product.Infrastructure.Products;

namespace Product.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContextPool<ProductDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductQueries, ProductQueries>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static async Task MigrateProductDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
