using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nac.Core.Modularity;
using Nac.MultiTenancy;
using Nac.MultiTenancy.Extensions;
using Nac.Observability;
using Nac.Observability.Extensions;
using Scalar.AspNetCore;

namespace Nac.WebApi.Extensions;

/// <summary>
/// Extension methods for configuring the NAC middleware pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Registers the NAC middleware pipeline in the correct order.
    /// Conditionally includes middleware based on registered modules and <see cref="NacWebApiOptions"/>.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same <see cref="WebApplication"/> for chaining.</returns>
    public static WebApplication UseNacApplication(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<NacWebApiOptions>>().Value;
        var factory = app.Services.GetRequiredService<NacApplicationFactory>();

        // 1. Exception handling — always
        app.UseExceptionHandler();

        // 2. HTTPS redirection — always
        app.UseHttpsRedirection();

        // 3. Response compression — conditional
        if (options.EnableResponseCompression)
            app.UseResponseCompression();

        // 4. Routing — always
        app.UseRouting();

        // 5. Rate limiter — conditional
        if (options.EnableRateLimiting)
            app.UseRateLimiter();

        // 6. CORS — conditional
        if (options.EnableCors)
            app.UseCors();

        // 7. Multi-tenancy — if module present
        if (HasModule<NacMultiTenancyModule>(factory))
            app.UseNacMultiTenancy();

        // 8. Authentication — always (no-op if no auth scheme)
        app.UseAuthentication();

        // 9. Authorization — always
        app.UseAuthorization();

        // 10. Observability — if module present
        if (HasModule<NacObservabilityModule>(factory))
            app.UseNacObservability();

        // 11. Controllers — always
        app.MapControllers();

        // 12. OpenAPI — conditional
        if (options.EnableOpenApi)
        {
            app.MapOpenApi();

            // 12a. Scalar UI — requires OpenAPI document
            if (options.EnableScalarUi)
            {
                app.MapScalarApiReference(opts =>
                {
                    options.ConfigureScalar?.Invoke(opts);
                });
            }
        }

        // 13. Health checks — conditional
        if (options.EnableHealthChecks)
            app.MapHealthChecks("/healthz");

        return app;
    }

    private static bool HasModule<TModule>(NacApplicationFactory factory) where TModule : NacModule =>
        factory.Modules.Any(m => m is TModule);
}
