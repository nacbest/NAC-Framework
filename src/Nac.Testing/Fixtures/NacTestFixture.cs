using Microsoft.Extensions.DependencyInjection;
using Nac.Caching;
using Nac.Core.Abstractions;
using Nac.Core.Abstractions.Identity;
using Nac.Core.Abstractions.Permissions;
using Nac.Cqrs.Dispatching;
using Nac.EventBus.Abstractions;
using Nac.Jobs.Abstractions;
using Nac.Testing.Fakes;

namespace Nac.Testing.Fixtures;

public class NacTestFixture : IDisposable
{
    private readonly ServiceProvider _provider;
    private bool _disposed;

    public FakeCurrentUser CurrentUser { get; }
    public FakeDateTimeProvider DateTimeProvider { get; }
    public FakePermissionChecker PermissionChecker { get; }
    public FakeEventPublisher EventPublisher { get; }
    public FakeSender Sender { get; }
    public FakeNacCache Cache { get; }
    public FakeJobScheduler JobScheduler { get; }
    public FakeRecurringJobManager RecurringJobManager { get; }

    public NacTestFixture()
    {
        CurrentUser = new FakeCurrentUser();
        DateTimeProvider = new FakeDateTimeProvider();
        PermissionChecker = FakePermissionChecker.GrantAll();
        EventPublisher = new FakeEventPublisher();
        Sender = new FakeSender();
        Cache = new FakeNacCache();
        JobScheduler = new FakeJobScheduler();
        RecurringJobManager = new FakeRecurringJobManager();

        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(CurrentUser);
        services.AddSingleton<IDateTimeProvider>(DateTimeProvider);
        services.AddSingleton<IPermissionChecker>(PermissionChecker);
        services.AddSingleton<IEventPublisher>(EventPublisher);
        services.AddSingleton<ISender>(Sender);
        services.AddSingleton<INacCache>(Cache);
        services.AddSingleton<IJobScheduler>(JobScheduler);
        services.AddSingleton<IRecurringJobManager>(RecurringJobManager);

        ConfigureServices(services);
        _provider = services.BuildServiceProvider();
    }

    /// <summary>Override to register additional services.</summary>
    protected virtual void ConfigureServices(IServiceCollection services) { }

    public T GetService<T>() where T : notnull =>
        _provider.GetRequiredService<T>();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _provider.Dispose();
        GC.SuppressFinalize(this);
    }
}
