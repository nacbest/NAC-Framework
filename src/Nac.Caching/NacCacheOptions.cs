namespace Nac.Caching;

/// <summary>
/// Global configuration options for the NAC caching layer.
/// Register via <c>AddNacCaching(options => ...)</c>.
/// </summary>
public sealed class NacCacheOptions
{
    /// <summary>
    /// Gets or sets the default expiration applied when <see cref="CacheEntryOptions.Expiration"/> is <see langword="null"/>.
    /// Defaults to 5 minutes.
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);
}
