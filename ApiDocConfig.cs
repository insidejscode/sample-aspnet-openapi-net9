using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

namespace SampleOpenApi.Config
{
    public static class ApiDocConfig
    {
        /// <summary>
        ///     Setup OpenApi + Versioning
        /// </summary>
        public static void ConfigureApiDocumentation(this IServiceCollection services)
        {
            // Add Versioning
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader(); //
            });
            services.AddVersionedApiExplorer(option =>
            {
                option.GroupNameFormat = "'v'VVV";
                option.SubstituteApiVersionInUrl = true; // if true - version is in url 
            });

            // Configure Open API
            //services.AddOpenApi("v1", options =>
            //{
            //    options.ShouldInclude = (description) => description.GroupName == "v1";
            //});

            services.AddOpenApi("v2", options =>
            {
                options.ShouldInclude = (description) => description.GroupName == "v2"; // condition to grab methods in document
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            });
        }

        // new 
        public static void UseApiDocumentation(this WebApplication app)
        {
            app.MapOpenApi("openapi/{documentName}.json");
            app.MapScalarApiReference(options =>
            {
                //options.AddDocument("v1");
                options.AddDocument("v2");
            });

            app.UseSwaggerUI(options =>
            {
                options.EnablePersistAuthorization();
                options.SwaggerEndpoint($"/openapi/v2.json", "v2");
                //options.SwaggerEndpoint($"/openapi/v1.json", "v1");
            });
        }


        /// <summary>
        ///     With IApplicationBilder - for Hosting flow
        ///     Use OpenAPI + SwaggerUI
        /// </summary>
        public static void UseApiDocumentation_Old(this IApplicationBuilder app /*, IApiVersionDescriptionProvider versionProvider */)
        {
            //var scalarDocuments = versionProvider.ApiVersionDescriptions.Select(x => new ScalarDocument(x.GroupName)
            //{
            //    Title = $"HSE API v{x.ApiVersion.Status}",
            //});

            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapOpenApi("openapi/{documentName}.json");
            //    endpoints.MapScalarApiReference(options =>
            //    {
            //        options.AddDocuments(scalarDocuments);
            //    });
            //});

            // manual
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapOpenApi("openapi/{documentName}.json");
                endpoints.MapScalarApiReference(options =>
                {
                    //options.AddDocument("v1");
                    options.AddDocument("v2");
                    options.Authentication = new ScalarAuthenticationOptions
                    {
                        PreferredSecuritySchemes = ["Bearer"],
                    };
                });
            });


            app.UseSwaggerUI(options =>
            {
                options.EnablePersistAuthorization();
                options.SwaggerEndpoint($"/openapi/v2.json", "v2");
                //options.SwaggerEndpoint($"/openapi/v1.json", "v1");

                //foreach (ApiVersionDescription description in provider.ApiVersionDescriptions)
                //{
                //    options.SwaggerEndpoint($"/openapi/{description.GroupName}.json", description.GroupName.ToUpperInvariant());
                //}
            });
        }


        private static void ConfigureOpenApi(IServiceCollection services)
        {
            // Authomatic dicsover api endpoints based on ApiVersions.All constains
            // With OpenApi there is no need of necessity of NSwag and Swashbuckle to generate the Swagger spec.
            //foreach (string version in ApiVersions.All)
            //{
            //    services.AddOpenApi($"v{version}", options =>
            //    {
            //        // Microsoft.AspNetCode.Mvc.Versioning
            //        // Microsoft.AspNetCode.Mvc.Versioning.ApiExplorer 
            //        // both packages use the Route.GroupName to determine which API version a particular path belongs to.
            //        options.ShouldInclude = (description) => description.GroupName == $"v{version}";
            //    });
            //}


            // Authomatic discover api endpoints, but IApiVersionDescriptionProvider as service in dependency is needed
            //var provider = services.BuildServiceProvider();
            //var service = provider.GetRequiredService<IApiVersionDescriptionProvider>();
            //foreach (ApiVersionDescription version in service.ApiVersionDescriptions.ToList())
            //{
            //    services.AddOpenApi($"v{version.GroupName}", options =>
            //    {
            //        // Microsoft.AspNetCode.Mvc.Versioning
            //        // Microsoft.AspNetCode.Mvc.Versioning.ApiExplorer 
            //        // both packages use the Route.GroupName to determine which API version a particular path belongs to.
            //        //options.ShouldInclude = (description) => description.GroupName == $"v{version.GroupName}";
            //    });
            //}

        }

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

    }

}
