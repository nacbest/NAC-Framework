namespace Nac.Core.Modularity;

public sealed class ApplicationShutdownContext(IServiceProvider serviceProvider)
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;
}
