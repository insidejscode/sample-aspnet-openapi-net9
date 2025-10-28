
## ASP.NET OpenApi setup.
Include: 

* ASP.NET with .NET 9 
* OpenApi 
* Swagger UI 
* Scalar 
* Authentication 
* Versioning

## Packages:
```
dotnet add package Microsoft.AspNetCore.Mvc.Versioning
dotnet add package Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer
dotnet add package Microsoft.AspNetCore.OpenApi
dotnet add package Scalar.AspNetCore
dotnet add package Swashbuckle.AspNetCore
```
## Main Key Points:

* Program.cs: 

```
var builder = WebApplication.CreateBuilder(args);

...

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader(); //
});
builder.Services.AddVersionedApiExplorer(option =>
{
    option.GroupNameFormat = "'v'VVV";
    option.SubstituteApiVersionInUrl = true; // if true - version is in url 
});

builder.Services.AddOpenApi("v2", options =>
{
    options.ShouldInclude = (description) => description.GroupName == "v2"; // condition to grab methods in document
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

...

var app = builder.Build();

...

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("openapi/{documentName}.json");
    app.MapScalarApiReference(options =>
    {
        options.AddDocument("v2");
        //options.AddDocument("v1");
    });

    app.UseSwaggerUI(options =>
    {
        options.EnablePersistAuthorization();
        options.SwaggerEndpoint($"/openapi/v2.json", "v2");
        //options.SwaggerEndpoint($"/openapi/v1.json", "v1");
    });
}
```

* TargetController.cs: 

```
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")] // version in route
[ApiController]
//[Route("[controller]")] // version in params:  http://localhost:5114/WeatherForecast?api-version=2
public class TargetController : ControllerBase
{
    ...
}
```


* BearerSecuritySchemeTransformer class:

    (https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/customize-openapi?view=aspnetcore-9.0#use-document-transformers)

```
// Add security metadata to OpenApi document
        internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
        {
            public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
            {
                var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
                if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
                {
                    var requirements = new Dictionary<string, OpenApiSecurityScheme>
                    {
                        ["Bearer"] = new OpenApiSecurityScheme
                        {
                            Type = SecuritySchemeType.Http,
                            Scheme = "bearer", // "bearer" refers to the header name here
                            In = ParameterLocation.Header,
                            BearerFormat = "Json Web Token"
                        }
                    };
                    document.Components ??= new OpenApiComponents();
                    document.Components.SecuritySchemes = requirements;

                    // Apply it as a requirement for all operations
                    foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
                    {
                        operation.Value.Security.Add(new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme } }] = Array.Empty<string>()
                        });
                    }
                }
            }
        }
```

