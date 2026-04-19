namespace Nac.Observability.Diagnostics;

/// <summary>
/// Well-known ActivitySource names for NAC Framework tracing.
/// Register with OpenTelemetry: builder.AddSource(NacActivitySources.All)
/// </summary>
public static class NacActivitySources
{
    public const string Cqrs = "Nac.Cqrs";
    public const string Persistence = "Nac.Persistence";
    public const string EventBus = "Nac.EventBus";
    public const string Identity = "Nac.Identity";
    public const string MultiTenancy = "Nac.MultiTenancy";
    public const string Caching = "Nac.Caching";
    public const string Jobs = "Nac.Jobs";

    /// <summary>
    /// All NAC ActivitySource names for bulk OTel registration.
    /// </summary>
    public static readonly string[] All =
        [Cqrs, Persistence, EventBus, Identity, MultiTenancy, Caching, Jobs];
}
