using Microsoft.Extensions.Hosting;

namespace Nac.Core.Modularity;

/// <summary>
/// Hosted service that calls <see cref="NacModule.OnApplicationInitialization"/>
/// on startup (sorted order) and <see cref="NacModule.OnApplicationShutdown"/>
/// on shutdown (reverse order).
/// </summary>
internal sealed class NacApplicationLifetime(
    NacApplicationFactory factory,
    IServiceProvider serviceProvider) : IHostedService
{
    private readonly IReadOnlyList<NacModule> _modules = factory.Modules;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var context = new ApplicationInitializationContext(serviceProvider);
        foreach (var module in _modules)
            module.OnApplicationInitialization(context);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        var context = new ApplicationShutdownContext(serviceProvider);
        for (var i = _modules.Count - 1; i >= 0; i--)
            _modules[i].OnApplicationShutdown(context);
        return Task.CompletedTask;
    }
}
