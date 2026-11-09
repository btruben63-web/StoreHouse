using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Product.Api.Controllers;

namespace Product.Api.UnitTest.Api;

public sealed class ProductsControllerAuthorizationTests
{
    private static AuthorizeAttribute? AuthorizeOf(string action) =>
        typeof(ProductsController).GetMethod(action)!.GetCustomAttribute<AuthorizeAttribute>();

    [Fact]
    public void Controller_HasNoClassLevelAuthorization() =>
        Assert.Null(typeof(ProductsController).GetCustomAttribute<AuthorizeAttribute>());

    [Theory]
    [InlineData(nameof(ProductsController.CreateProduct))]
    [InlineData(nameof(ProductsController.DeleteProduct))]
    public void CreateAndDelete_RequireAdminRole(string action)
    {
        var authorize = AuthorizeOf(action);

        Assert.NotNull(authorize);
        Assert.Equal("Admin", authorize!.Roles);
    }

    [Theory]
    [InlineData(nameof(ProductsController.UpdateProduct))]
    [InlineData(nameof(ProductsController.AddStockToProduct))]
    [InlineData(nameof(ProductsController.DecreaseStockToProduct))]
    public void UpdateAndStock_RequireAuthenticatedUserWithoutRole(string action)
    {
        var authorize = AuthorizeOf(action);

        Assert.NotNull(authorize);
        Assert.True(string.IsNullOrEmpty(authorize!.Roles));
    }

    [Theory]
    [InlineData(nameof(ProductsController.GetProducts))]
    [InlineData(nameof(ProductsController.GetProductById))]
    [InlineData(nameof(ProductsController.SearchProductsByName))]
    [InlineData(nameof(ProductsController.SearchProductsByStockRange))]
    public void Reads_ArePublic(string action) => Assert.Null(AuthorizeOf(action));
}
