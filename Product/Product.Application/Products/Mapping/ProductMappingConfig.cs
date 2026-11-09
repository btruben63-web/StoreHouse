using Mapster;
using Product.Application.Products.DTOs;

namespace Product.Application.Products.Mapping;

public sealed class ProductMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config) =>
        config.NewConfig<ProductEntity, ProductDTO>()
            .Map(dest => dest.StockQuantity, src => src.Stock != null ? src.Stock.Quantity : 0);
}
