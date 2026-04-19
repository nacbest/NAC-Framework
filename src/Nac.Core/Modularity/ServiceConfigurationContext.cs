using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Nac.Core.Modularity;

public sealed class ServiceConfigurationContext(IServiceCollection services, IConfiguration configuration)
{
    public IServiceCollection Services { get; } = services;
    public IConfiguration Configuration { get; } = configuration;
}
