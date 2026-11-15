using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RotaLeve.Api.Presentation.Swagger;

/// <summary>
/// Alinha os cadeados do Swagger à autorização real do ASP.NET Core.
/// FallbackPolicy exige token por padrão, exceto [AllowAnonymous].
/// </summary>
public sealed class JwtAuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var controllerAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
            ?? Array.Empty<object>();
        var actionAttributes = context.MethodInfo.GetCustomAttributes(true);
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        if (controllerAttributes.OfType<IAllowAnonymous>().Any()
            || actionAttributes.OfType<IAllowAnonymous>().Any()
            || metadata.OfType<IAllowAnonymous>().Any())
        {
            operation.Security = new List<OpenApiSecurityRequirement>();
            return;
        }

        // Todas as demais operações usam JWT por padrão; papéis são verificados pelo backend.
        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new()
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                }] = Array.Empty<string>()
            }
        };
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Token JWT ausente, expirado ou inválido" });
    }
}
