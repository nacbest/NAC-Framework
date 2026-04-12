using Microsoft.AspNetCore.Builder;

namespace Nac.WebApi.Extensions;

/// <summary>
/// Pipeline extension for the NAC WebApi layer.
/// </summary>
public static class WebApiServiceCollectionExtensions
{
    /// <summary>
    /// Adds the NAC global exception handler to the pipeline.
    /// Must be called early in the middleware pipeline to catch all exceptions.
    /// </summary>
    /// <example>
    /// <code>
    /// var app = builder.Build();
    /// app.UseNacWebApi();       // exception handler first
    /// app.UseNacFramework();    // module endpoints
    /// app.Run();
    /// </code>
    /// </example>
    public static IApplicationBuilder UseNacWebApi(this IApplicationBuilder app)
    {
        app.UseMiddleware<GlobalExceptionHandler>();
        return app;
    }
}
