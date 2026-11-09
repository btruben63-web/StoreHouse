using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Product.Api.Swagger;

internal sealed class AuthorizeOperationFilter : IOperationFilter
{
    public const string SchemeName = "Bearer";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var authorizeData = metadata.OfType<IAuthorizeData>().ToList();
        if (authorizeData.Count == 0 || metadata.OfType<IAllowAnonymous>().Any())
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeName, context.Document)] = []
        });

        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
        if (authorizeData.Any(data => !string.IsNullOrEmpty(data.Roles)))
        {
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });
        }
    }
}
