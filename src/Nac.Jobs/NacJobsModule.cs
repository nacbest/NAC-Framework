namespace Nac.Jobs;

using Nac.Core.Modularity;
using Nac.Jobs.Extensions;

/// <summary>
/// NAC Jobs module — background job scheduling abstractions.
/// </summary>
[DependsOn(typeof(NacCoreModule))]
public sealed class NacJobsModule : NacModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddNacJobs();
    }
}
