namespace Nac.Core.Modularity;

public sealed class ApplicationInitializationContext(IServiceProvider serviceProvider)
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;
}
