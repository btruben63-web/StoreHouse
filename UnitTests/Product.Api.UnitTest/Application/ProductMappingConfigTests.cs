using Product.Api.UnitTest.TestData;
using Product.Application.Products.DTOs;

namespace Product.Api.UnitTest.Application;

public sealed class ProductMappingConfigTests
{
    [Fact]
    public void MapsEntityToDTO_IncludingStockQuantity()
    {
        var dto = TestMapping.CreateMapper().Map<ProductDTO>(TestProducts.Hammer(stock: 40));

        Assert.Equal(TestProducts.HammerDTO, dto);
    }

    [Fact]
    public void EntityWithoutStock_MapsZeroQuantity()
    {
        var product = TestProducts.Hammer();
        product.Stock = null;

        var dto = TestMapping.CreateMapper().Map<ProductDTO>(product);

        Assert.Equal(0, dto.StockQuantity);
    }
}
