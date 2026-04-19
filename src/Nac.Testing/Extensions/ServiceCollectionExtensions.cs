using Microsoft.Extensions.DependencyInjection;
using Nac.Caching;
using Nac.Core.Abstractions;
using Nac.Core.Abstractions.Identity;
using Nac.Core.Abstractions.Permissions;
using Nac.Cqrs.Dispatching;
using Nac.EventBus.Abstractions;
using Nac.Jobs.Abstractions;
using Nac.Testing.Fakes;

namespace Nac.Testing.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNacTesting(this IServiceCollection services)
    {
        services.AddSingleton<FakeCurrentUser>();
        services.AddSingleton<ICurrentUser>(sp => sp.GetRequiredService<FakeCurrentUser>());
        services.AddSingleton<FakeDateTimeProvider>();
        services.AddSingleton<IDateTimeProvider>(sp => sp.GetRequiredService<FakeDateTimeProvider>());
        services.AddSingleton(FakePermissionChecker.GrantAll());
        services.AddSingleton<IPermissionChecker>(sp => sp.GetRequiredService<FakePermissionChecker>());
        services.AddSingleton<FakeEventPublisher>();
        services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<FakeEventPublisher>());
        services.AddSingleton<FakeSender>();
        services.AddSingleton<ISender>(sp => sp.GetRequiredService<FakeSender>());
        services.AddSingleton<FakeNacCache>();
        services.AddSingleton<INacCache>(sp => sp.GetRequiredService<FakeNacCache>());
        services.AddSingleton<FakeJobScheduler>();
        services.AddSingleton<IJobScheduler>(sp => sp.GetRequiredService<FakeJobScheduler>());
        services.AddSingleton<FakeRecurringJobManager>();
        services.AddSingleton<IRecurringJobManager>(sp => sp.GetRequiredService<FakeRecurringJobManager>());
        return services;
    }
}
