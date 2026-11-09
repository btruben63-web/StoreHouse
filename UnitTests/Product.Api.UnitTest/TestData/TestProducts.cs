using Microsoft.Extensions.Time.Testing;
using Product.Application.Products.DTOs;
using Product.Domain.Entities;

namespace Product.Api.UnitTest.TestData;

internal static class TestProducts
{
    public static readonly DateTime Now = new(2026, 11, 9, 10, 0, 0);
    public static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0);

    public static FakeTimeProvider CreateTimeProvider() => new(new DateTimeOffset(Now, TimeSpan.Zero));

    public static ProductDTO HammerDTO => new(100001, "Martelo de Carpinteiro", "Martelo com cabo de madeira.", 12.90m, 40);

    public static ProductEntity Hammer(int stock = 40) => new()
    {
        Id = 100001,
        Name = "Martelo de Carpinteiro",
        Description = "Martelo com cabo de madeira.",
        Price = 12.90m,
        CreatedAt = SeedDate,
        Stock = new Stock { ProductId = 100001, Quantity = stock, UpdatedAt = SeedDate }
    };
}
