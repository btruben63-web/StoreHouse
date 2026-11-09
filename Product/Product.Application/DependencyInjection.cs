using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Product.Application.Common.Behaviors;

namespace Product.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        var mappingConfig = new TypeAdapterConfig();
        mappingConfig.Scan(assembly);
        services.AddSingleton(mappingConfig);
        services.AddSingleton<IMapper>(new Mapper(mappingConfig));
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
