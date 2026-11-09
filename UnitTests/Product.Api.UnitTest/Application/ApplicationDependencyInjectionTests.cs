using FluentValidation;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Product.Application;
using Product.Application.Common.Results;
using Product.Application.Products.Commands.CreateProduct;
using Product.Application.Products.DTOs;
using Product.Application.Products.Queries.GetProductById;

namespace Product.Api.UnitTest.Application;

public sealed class ApplicationDependencyInjectionTests
{
    [Fact]
    public void AddApplication_RegistersMediatorValidatorsMapperTimeProviderAndBehaviors()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        Assert.NotNull(scopedProvider.GetService<ISender>());
        Assert.NotNull(scopedProvider.GetService<IMapper>());
        Assert.NotNull(scopedProvider.GetService<TimeProvider>());
        Assert.NotNull(scopedProvider.GetService<IValidator<CreateProductCommand>>());
        Assert.Equal(2, scopedProvider.GetServices<IPipelineBehavior<GetProductByIdQuery, Result<ProductDTO>>>().Count());
    }
}
