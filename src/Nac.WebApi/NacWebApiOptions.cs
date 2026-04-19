using Asp.Versioning;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;

namespace Nac.WebApi;

/// <summary>
/// Configuration options for the NAC WebApi module.
/// Controls which middleware and services are registered.
/// </summary>
public sealed class NacWebApiOptions
{
    /// <summary>Enable API versioning via Asp.Versioning. Default: true.</summary>
    public bool EnableApiVersioning { get; set; } = true;

    /// <summary>Enable OpenAPI (Swagger) endpoint. Default: true.</summary>
    public bool EnableOpenApi { get; set; } = true;

    /// <summary>Enable Scalar API reference UI at /scalar/v1. Requires <see cref="EnableOpenApi"/>. Default: true.</summary>
    public bool EnableScalarUi { get; set; } = true;

    /// <summary>Optional Scalar UI configuration callback.</summary>
    public Action<ScalarOptions>? ConfigureScalar { get; set; }

    /// <summary>Enable CORS middleware. Default: true.</summary>
    public bool EnableCors { get; set; } = true;

    /// <summary>Enable rate limiting middleware. Default: false.</summary>
    public bool EnableRateLimiting { get; set; }

    /// <summary>Enable Brotli + Gzip response compression. Default: true.</summary>
    public bool EnableResponseCompression { get; set; } = true;

    /// <summary>Enable health check endpoint at /healthz. Default: true.</summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>Optional CORS configuration callback.</summary>
    public Action<CorsOptions>? ConfigureCors { get; set; }

    /// <summary>Optional rate limiter configuration callback.</summary>
    public Action<RateLimiterOptions>? ConfigureRateLimiter { get; set; }

    /// <summary>Optional API versioning configuration callback.</summary>
    public Action<ApiVersioningOptions>? ConfigureApiVersioning { get; set; }
}
