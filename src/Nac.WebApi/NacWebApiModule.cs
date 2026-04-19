using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nac.Core.Modularity;
using Nac.Observability;
using Nac.WebApi.ExceptionHandling;

namespace Nac.WebApi;

/// <summary>
/// NAC WebApi composition root module. Registers web infrastructure services:
/// exception handling, API versioning, OpenAPI, CORS, rate limiting,
/// response compression, health checks, and controllers.
/// </summary>
[DependsOn(typeof(NacCoreModule))]
[DependsOn(typeof(NacObservabilityModule))]
public sealed class NacWebApiModule : NacModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        // Read options configured by AddNacWebApi(). Build a temporary provider
        // only to read the pre-configured options — standard pattern for module systems.
#pragma warning disable ASP0000
        using var tempProvider = services.BuildServiceProvider();
#pragma warning restore ASP0000
        var options = tempProvider.GetService<IOptions<NacWebApiOptions>>()?.Value
            ?? new NacWebApiOptions();

        // Exception handling — always registered
        services.AddProblemDetails();
        services.AddExceptionHandler<NacExceptionHandler>();

        // Controllers — always registered
        services.AddControllers();

        // API Versioning
        if (options.EnableApiVersioning)
        {
            var versioningBuilder = services.AddApiVersioning(opts =>
            {
                opts.DefaultApiVersion = new ApiVersion(1, 0);
                opts.AssumeDefaultVersionWhenUnspecified = true;
                opts.ReportApiVersions = true;
                options.ConfigureApiVersioning?.Invoke(opts);
            });
            versioningBuilder.AddApiExplorer(opts =>
            {
                opts.GroupNameFormat = "'v'VVV";
                opts.SubstituteApiVersionInUrl = true;
            });
        }

        // OpenAPI
        if (options.EnableOpenApi)
            services.AddOpenApi();

        // CORS
        if (options.EnableCors)
        {
            services.AddCors(cors =>
            {
                if (options.ConfigureCors is not null)
                    options.ConfigureCors(cors);
                else
                    cors.AddDefaultPolicy(policy => policy
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
            });
        }

        // Rate Limiting
        if (options.EnableRateLimiting)
            services.AddRateLimiter(options.ConfigureRateLimiter ?? (_ => { }));

        // Response Compression
        if (options.EnableResponseCompression)
        {
            services.AddResponseCompression(opts =>
            {
                opts.EnableForHttps = true;
                opts.Providers.Add<BrotliCompressionProvider>();
                opts.Providers.Add<GzipCompressionProvider>();
            });
        }

        // Health Checks
        if (options.EnableHealthChecks)
            services.AddHealthChecks();
    }
}
