namespace Nac.Observability.Extensions;

using Microsoft.AspNetCore.Builder;
using Nac.Observability.Logging;

/// <summary>
/// Extension methods for registering Nac.Observability middleware.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds LoggingEnricherMiddleware to the pipeline.
    /// Place after UseAuthentication() and UseNacMultiTenancy().
    /// </summary>
    public static IApplicationBuilder UseNacObservability(this IApplicationBuilder app)
    {
        app.UseMiddleware<LoggingEnricherMiddleware>();
        return app;
    }
}
