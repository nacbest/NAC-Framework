namespace Nac.Observability.Diagnostics;

/// <summary>
/// Well-known Meter names for NAC Framework metrics.
/// Register with OpenTelemetry: builder.AddMeter(NacMeters.All)
/// </summary>
public static class NacMeters
{
    public const string Cqrs = "Nac.Cqrs";
    public const string Persistence = "Nac.Persistence";
    public const string EventBus = "Nac.EventBus";
    public const string Caching = "Nac.Caching";
    public const string Jobs = "Nac.Jobs";

    /// <summary>
    /// All NAC Meter names for bulk OTel registration.
    /// </summary>
    public static readonly string[] All = [Cqrs, Persistence, EventBus, Caching, Jobs];
}
