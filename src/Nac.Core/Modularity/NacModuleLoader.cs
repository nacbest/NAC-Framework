namespace Nac.Core.Modularity;

/// <summary>
/// Discovers module types by walking the <see cref="DependsOnAttribute"/> graph
/// from a startup module, then returns them in topologically sorted order.
/// </summary>
public static class NacModuleLoader
{
    /// <summary>
    /// Discovers and instantiates all modules reachable from <typeparamref name="TStartup"/>,
    /// sorted so that dependencies come before dependents.
    /// </summary>
    public static IReadOnlyList<NacModule> LoadModules<TStartup>() where TStartup : NacModule
    {
        return LoadModules(typeof(TStartup));
    }

    /// <summary>
    /// Discovers and instantiates all modules reachable from <paramref name="startupModuleType"/>,
    /// sorted so that dependencies come before dependents.
    /// </summary>
    public static IReadOnlyList<NacModule> LoadModules(Type startupModuleType)
    {
        var moduleTypes = DiscoverModuleTypes(startupModuleType);
        var sorted = TopologicalSort(moduleTypes);
        return sorted.Select(t => (NacModule)Activator.CreateInstance(t)!).ToList();
    }

    private static HashSet<Type> DiscoverModuleTypes(Type startupType)
    {
        var visited = new HashSet<Type>();
        var queue = new Queue<Type>();
        queue.Enqueue(startupType);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current)) continue;

            if (!current.IsAssignableTo(typeof(NacModule)))
                throw new InvalidOperationException(
                    $"Type '{current.FullName}' is declared as a dependency but does not extend NacModule.");

            foreach (var dep in GetDependencies(current))
                queue.Enqueue(dep);
        }

        return visited;
    }

    /// <summary>
    /// Kahn's algorithm: iteratively removes nodes with zero in-degree.
    /// Detects cycles when remaining nodes still have edges.
    /// </summary>
    private static List<Type> TopologicalSort(HashSet<Type> moduleTypes)
    {
        // Build in-degree map and adjacency list (dep → dependents)
        var inDegree = new Dictionary<Type, int>();
        var dependents = new Dictionary<Type, List<Type>>();

        foreach (var type in moduleTypes)
        {
            inDegree[type] = 0;
            dependents[type] = [];
        }

        foreach (var type in moduleTypes)
        {
            var deps = GetDependencies(type).Where(moduleTypes.Contains).ToList();
            inDegree[type] = deps.Count;
            foreach (var dep in deps)
                dependents[dep].Add(type);
        }

        var queue = new Queue<Type>(moduleTypes.Where(t => inDegree[t] == 0));
        var sorted = new List<Type>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            sorted.Add(current);

            foreach (var dependent in dependents[current])
            {
                inDegree[dependent]--;
                if (inDegree[dependent] == 0)
                    queue.Enqueue(dependent);
            }
        }

        if (sorted.Count != moduleTypes.Count)
        {
            var cycleTypes = moduleTypes.Where(t => inDegree[t] > 0).Select(t => t.Name);
            throw new InvalidOperationException(
                $"Circular module dependency detected among: {string.Join(" \u2192 ", cycleTypes)}");
        }

        return sorted;
    }

    private static IEnumerable<Type> GetDependencies(Type moduleType)
    {
        return moduleType
            .GetCustomAttributes(typeof(DependsOnAttribute), false)
            .Cast<DependsOnAttribute>()
            .SelectMany(a => a.DependedModuleTypes);
    }
}
