using Product.Domain.Entities;
using Product.Domain.Enums;

namespace Product.Infrastructure.Persistence.Seed;

internal static class ProductSeedData
{
    private const string SeedUser = "seed";
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0);

    private static readonly (int Id, string Name, string Description, decimal Price, int Quantity)[] Items =
    [
        (100001, "Martelo de Carpinteiro", "Martelo com cabo de madeira e cabeça de aço forjado.", 12.90m, 40),
        (100002, "Berbequim sem Fios 18V", "Berbequim com bateria de lítio e duas velocidades.", 89.99m, 15),
        (100003, "Fita Métrica 5m", "Fita métrica com travão e caixa antichoque.", 6.50m, 120),
        (100004, "Alicate Universal", "Alicate de 180 mm com punhos isolados.", 9.75m, 60),
        (100005, "Serrote de Costas", "Serrote para cortes de precisão em madeira.", 14.20m, 25),
        (100006, "Nível de Bolha 60cm", "Nível em alumínio com três bolhas.", 11.40m, 30),
        (100007, "Jogo de Chaves de Fendas", "Conjunto de seis chaves planas e Phillips.", 18.90m, 45),
        (100008, "Caixa de Parafusos 200un", "Parafusos para madeira de vários tamanhos.", 7.30m, 200),
        (100009, "Lixa para Madeira P120", "Folha de lixa de grão médio.", 1.20m, 500),
        (100010, "Cola de Madeira 750ml", "Cola vinílica de secagem rápida.", 8.60m, 80)
    ];

    public static IEnumerable<ProductEntity> Products => Items.Select(item => new ProductEntity
    {
        Id = item.Id,
        Name = item.Name,
        Description = item.Description,
        Price = item.Price,
        CreatedAt = SeedDate
    });

    public static IEnumerable<Stock> Stocks => Items.Select(item => new Stock
    {
        ProductId = item.Id,
        Quantity = item.Quantity,
        UpdatedAt = SeedDate
    });

    public static IEnumerable<StockMovement> Movements => Items.Select((item, index) => new StockMovement
    {
        Id = index + 1,
        ProductId = item.Id,
        Type = StockMovementType.Initial,
        Quantity = item.Quantity,
        QuantityAfter = item.Quantity,
        CreatedAt = SeedDate,
        CreatedBy = SeedUser
    });
}
