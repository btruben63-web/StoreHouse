using Microsoft.OpenApi;

namespace Product.Api.Swagger;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "StoreHouse Product API", Version = "v1" });
            options.AddSecurityDefinition(AuthorizeOperationFilter.SchemeName, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the accessToken returned by POST /api/auth/login on App.IdentityServer."
            });
            options.OperationFilter<AuthorizeOperationFilter>();
        });

        return services;
    }
}
