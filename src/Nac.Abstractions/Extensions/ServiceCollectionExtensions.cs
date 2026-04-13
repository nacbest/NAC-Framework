using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Nac.Abstractions.Modularity;

namespace Nac.Abstractions.Extensions;

/// <summary>
/// Extension methods for composing the NAC framework at the application entry point.
/// </summary>
public static class NacServiceCollectionExtensions
{
    /// <summary>
    /// Adds the NAC framework to the application. Use the builder to register modules and features.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.AddNacFramework(nac =>
    /// {
    ///     nac.AddModule&lt;CatalogModule&gt;();
    ///     nac.AddModule&lt;OrdersModule&gt;();
    /// });
    /// </code>
    /// </example>
    public static WebApplicationBuilder AddNacFramework(
        this WebApplicationBuilder builder,
        Action<NacFrameworkBuilder> configure)
    {
        var nacBuilder = new NacFrameworkBuilder(builder.Services, builder.Configuration);
        configure(nacBuilder);

        ValidateModuleDependencies(nacBuilder.Modules);

        foreach (var module in nacBuilder.Modules)
        {
            module.ConfigureServices(builder.Services, builder.Configuration);
        }

        builder.Services.AddSingleton(nacBuilder.Modules);

        return builder;
    }

    /// <summary>
    /// Applies NAC framework middleware and maps module endpoints.
    /// Call after <c>builder.Build()</c>.
    /// </summary>
    public static WebApplication UseNacFramework(this WebApplication app)
    {
        var modules = app.Services.GetRequiredService<IReadOnlyList<INacModule>>();

        foreach (var module in modules)
        {
            module.ConfigurePipeline(app);
        }

        foreach (var module in modules)
        {
            module.ConfigureEndpoints(app);
        }

        return app;
    }

    private static void ValidateModuleDependencies(IReadOnlyList<INacModule> modules)
    {
        var registeredTypes = new HashSet<Type>(modules.Select(m => m.GetType()));

        foreach (var module in modules)
        {
            foreach (var dep in module.Dependencies)
            {
                if (!registeredTypes.Contains(dep))
                {
                    throw new InvalidOperationException(
                        $"Module '{module.Name}' depends on '{dep.Name}' which is not registered. " +
                        $"Register it via AddModule<{dep.Name}>() before or after '{module.Name}'.");
                }
            }
        }

        DetectCircularDependencies(modules);
    }

    private static void DetectCircularDependencies(IReadOnlyList<INacModule> modules)
    {
        var graph = modules.ToDictionary(m => m.GetType(), m => m.Dependencies);
        var visited = new HashSet<Type>();
        var visiting = new HashSet<Type>();

        foreach (var moduleType in graph.Keys)
        {
            if (HasCycle(moduleType, graph, visited, visiting))
            {
                throw new InvalidOperationException(
                    $"Circular dependency detected involving module '{moduleType.Name}'. " +
                    "Module dependencies must form a directed acyclic graph.");
            }
        }
    }

    private static bool HasCycle(
        Type current,
        Dictionary<Type, IReadOnlyList<Type>> graph,
        HashSet<Type> visited,
        HashSet<Type> visiting)
    {
        if (visiting.Contains(current)) return true;
        if (visited.Contains(current)) return false;

        visiting.Add(current);

        if (graph.TryGetValue(current, out var deps))
        {
            foreach (var dep in deps)
            {
                if (HasCycle(dep, graph, visited, visiting))
                    return true;
            }
        }

        visiting.Remove(current);
        visited.Add(current);
        return false;
    }
}
