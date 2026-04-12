using System.CommandLine;
using System.Text.Json;
using Nac.Cli.Templates;

namespace Nac.Cli.Commands;

/// <summary>
/// <c>nac add module|feature|entity</c> — scaffolds modules, features, and entities.
/// </summary>
internal static class AddCommand
{
    public static Command Create()
    {
        var cmd = new Command("add", "Add a module, feature, or entity");
        cmd.Add(CreateModuleCommand());
        cmd.Add(CreateFeatureCommand());
        cmd.Add(CreateEntityCommand());
        return cmd;
    }

    private static Command CreateModuleCommand()
    {
        var nameArg = new Argument<string>("name") { Description = "Module name (PascalCase)" };
        var cmd = new Command("module", "Add a new module to the solution") { nameArg };
        cmd.SetAction(async (parseResult, ct) =>
        {
            var name = parseResult.GetValue(nameArg)!;
            await AddModuleAsync(name);
        });
        return cmd;
    }

    private static Command CreateFeatureCommand()
    {
        var pathArg = new Argument<string>("path") { Description = "Module/Feature (e.g., Catalog/CreateProduct)" };
        var cmd = new Command("feature", "Add command + handler + endpoint") { pathArg };
        cmd.SetAction(async (parseResult, ct) =>
        {
            var path = parseResult.GetValue(pathArg)!;
            await AddFeatureAsync(path);
        });
        return cmd;
    }

    private static Command CreateEntityCommand()
    {
        var pathArg = new Argument<string>("path") { Description = "Module/Entity (e.g., Catalog/Product)" };
        var cmd = new Command("entity", "Add a domain entity") { pathArg };
        cmd.SetAction(async (parseResult, ct) =>
        {
            var path = parseResult.GetValue(pathArg)!;
            await AddEntityAsync(path);
        });
        return cmd;
    }

    private static async Task AddModuleAsync(string name)
    {
        var (ns, _) = ReadManifest();
        var moduleDir = Path.Combine("src", "Modules", $"{ns}.Modules.{name}");

        if (Directory.Exists(moduleDir))
        {
            Console.Error.WriteLine($"Module '{name}' already exists.");
            return;
        }

        Directory.CreateDirectory(Path.Combine(moduleDir, "Domain"));
        Directory.CreateDirectory(Path.Combine(moduleDir, "Application", "Commands"));
        Directory.CreateDirectory(Path.Combine(moduleDir, "Application", "Queries"));
        Directory.CreateDirectory(Path.Combine(moduleDir, "Infrastructure"));
        Directory.CreateDirectory(Path.Combine(moduleDir, "Endpoints"));

        await File.WriteAllTextAsync(
            Path.Combine(moduleDir, $"{ns}.Modules.{name}.csproj"),
            CodeTemplates.ModuleCsproj(ns, name));

        await File.WriteAllTextAsync(
            Path.Combine(moduleDir, $"{name}Module.cs"),
            CodeTemplates.ModuleClass(ns, name));

        Console.WriteLine($"Created module '{name}' at {moduleDir}");
        Console.WriteLine($"  Domain/           — entities, value objects");
        Console.WriteLine($"  Application/      — commands, queries, handlers");
        Console.WriteLine($"  Infrastructure/   — persistence, external services");
        Console.WriteLine($"  Endpoints/        — minimal API endpoints");
        Console.WriteLine();
        Console.WriteLine($"Next: nac add feature {name}/<FeatureName>");
    }

    private static async Task AddFeatureAsync(string path)
    {
        var (module, feature) = ParseModulePath(path);
        var (ns, _) = ReadManifest();
        var moduleDir = FindModuleDir(ns, module);

        var cmdDir = Path.Combine(moduleDir, "Application", "Commands");
        var endpointDir = Path.Combine(moduleDir, "Endpoints");
        Directory.CreateDirectory(cmdDir);
        Directory.CreateDirectory(endpointDir);

        await File.WriteAllTextAsync(
            Path.Combine(cmdDir, $"{feature}Command.cs"),
            CodeTemplates.CommandFile(ns, module, feature));

        await File.WriteAllTextAsync(
            Path.Combine(cmdDir, $"{feature}Handler.cs"),
            CodeTemplates.HandlerFile(ns, module, feature));

        await File.WriteAllTextAsync(
            Path.Combine(endpointDir, $"{feature}Endpoint.cs"),
            CodeTemplates.EndpointFile(ns, module, feature));

        Console.WriteLine($"Created feature '{feature}' in module '{module}':");
        Console.WriteLine($"  Application/Commands/{feature}Command.cs");
        Console.WriteLine($"  Application/Commands/{feature}Handler.cs");
        Console.WriteLine($"  Endpoints/{feature}Endpoint.cs");
    }

    private static async Task AddEntityAsync(string path)
    {
        var (module, entity) = ParseModulePath(path);
        var (ns, _) = ReadManifest();
        var moduleDir = FindModuleDir(ns, module);

        var domainDir = Path.Combine(moduleDir, "Domain");
        Directory.CreateDirectory(domainDir);

        await File.WriteAllTextAsync(
            Path.Combine(domainDir, $"{entity}.cs"),
            CodeTemplates.EntityFile(ns, module, entity));

        Console.WriteLine($"Created entity '{entity}' in module '{module}':");
        Console.WriteLine($"  Domain/{entity}.cs");
    }

    private static (string Module, string Name) ParseModulePath(string path)
    {
        var parts = path.Split('/');
        if (parts.Length != 2)
            throw new ArgumentException($"Expected format 'Module/Name', got '{path}'");
        return (parts[0], parts[1]);
    }

    private static (string Namespace, string Name) ReadManifest()
    {
        var manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "nac.json");
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException("nac.json not found. Run 'nac new' first.");

        using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var solution = doc.RootElement.GetProperty("solution");
        var name = solution.GetProperty("name").GetString()!;
        var ns = solution.GetProperty("namespace").GetString() ?? name;
        return (ns, name);
    }

    private static string FindModuleDir(string ns, string module)
    {
        var dir = Path.Combine("src", "Modules", $"{ns}.Modules.{module}");
        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException(
                $"Module '{module}' not found at {dir}. Run 'nac add module {module}' first.");
        return dir;
    }
}
