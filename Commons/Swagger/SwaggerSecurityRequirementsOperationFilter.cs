using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace stockmind.Commons.Swagger;

public class SwaggerSecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var hasAuthorize = context.MethodInfo
                              .GetCustomAttributes(true)
                              .OfType<AuthorizeAttribute>()
                              .Any()
                          || (context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
                                  .OfType<AuthorizeAttribute>()
                                  .Any() ?? false);

        if (!hasAuthorize)
        {
            return;
        }

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    }
}
