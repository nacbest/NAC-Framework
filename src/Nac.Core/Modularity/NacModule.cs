namespace Nac.Core.Modularity;

public abstract class NacModule
{
    public virtual void PreConfigureServices(ServiceConfigurationContext context) { }
    public virtual void ConfigureServices(ServiceConfigurationContext context) { }
    public virtual void PostConfigureServices(ServiceConfigurationContext context) { }
    public virtual void OnApplicationInitialization(ApplicationInitializationContext context) { }
    public virtual void OnApplicationShutdown(ApplicationShutdownContext context) { }
}
