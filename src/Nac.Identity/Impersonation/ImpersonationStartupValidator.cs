using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Nac.Identity.Impersonation;

/// <summary>
/// Fails application startup when <see cref="IImpersonationRoleProvider"/> is not
/// registered. Consumer must call <c>services.AddScoped&lt;IImpersonationRoleProvider, …&gt;</c>
/// before calling <c>AddNacIdentity</c>; otherwise the impersonation endpoint would
/// always 500 at runtime. Fail-fast is clearer than first-request failure.
/// </summary>
internal sealed class ImpersonationStartupValidator(IServiceProvider services) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider.GetService<IImpersonationRoleProvider>();
        if (provider is null)
        {
            throw new InvalidOperationException(
                "Host impersonation requires IImpersonationRoleProvider to be registered. " +
                "Register a consumer implementation before AddNacIdentity(), " +
                "e.g. services.AddScoped<IImpersonationRoleProvider, MyImpersonationRoleProvider>().");
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
