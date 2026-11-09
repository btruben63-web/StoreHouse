using Mapster;
using MapsterMapper;
using Product.Application.Products.Mapping;

namespace Product.Api.UnitTest.TestData;

internal static class TestMapping
{
    public static IMapper CreateMapper()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(ProductMappingConfig).Assembly);
        return new Mapper(config);
    }
}
