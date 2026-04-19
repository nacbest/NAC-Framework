namespace Nac.Jobs.Extensions;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// DI extension for Nac.Jobs registration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Nac.Jobs module. Consumer must register IJobScheduler
    /// and IRecurringJobManager implementations separately.
    /// </summary>
    public static IServiceCollection AddNacJobs(this IServiceCollection services)
    {
        // No default registrations — consumer provides implementations
        return services;
    }
}
