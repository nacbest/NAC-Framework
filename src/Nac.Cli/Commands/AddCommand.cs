using System.CommandLine;
using System.Diagnostics;
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
        var (ns, solutionName, localNacPath) = ReadManifest();
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
            CodeTemplates.ModuleCsproj(ns, name, localNacPath));

        await File.WriteAllTextAsync(
            Path.Combine(moduleDir, $"{name}Module.cs"),
            CodeTemplates.ModuleClass(ns, name));

        // Add project to solution
        var csprojPath = Path.Combine(moduleDir, $"{ns}.Modules.{name}.csproj");
        var slnFile = Directory.GetFiles(".", "*.sln*").FirstOrDefault();
        if (slnFile != null)
        {
            await RunDotnetAsync($"sln \"{slnFile}\" add \"{csprojPath}\"");
        }

        // Add ProjectReference to Host.csproj
        var hostCsprojPath = Path.Combine("src", $"{ns}.Host", $"{ns}.Host.csproj");
        if (File.Exists(hostCsprojPath))
        {
            await AddProjectReferenceToHostAsync(hostCsprojPath, ns, name);
        }

        // Update Program.cs to register module
        var programPath = Path.Combine("src", $"{ns}.Host", "Program.cs");
        if (File.Exists(programPath))
        {
            await UpdateProgramCsAsync(programPath, ns, name);
        }

        // Verify build (with restore for new projects)
        Console.WriteLine();
        Console.WriteLine("Verifying build...");
        var buildSuccess = await RunDotnetAsync("build -v q");

        Console.WriteLine();
        Console.WriteLine($"Created module '{name}' at {moduleDir}");
        Console.WriteLine($"  Domain/           — entities, value objects");
        Console.WriteLine($"  Application/      — commands, queries, handlers");
        Console.WriteLine($"  Infrastructure/   — persistence, external services");
        Console.WriteLine($"  Endpoints/        — minimal API endpoints");

        if (buildSuccess)
            Console.WriteLine($"  ✓ Build verified");
        else
            Console.WriteLine($"  ✗ Build failed — check errors above");

        Console.WriteLine();
        Console.WriteLine($"Next: nac add feature {name}/<FeatureName>");
    }

    private static async Task<bool> RunDotnetAsync(string args)
    {
        var psi = new ProcessStartInfo("dotnet", args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var proc = Process.Start(psi);
        var output = await proc!.StandardOutput.ReadToEndAsync();
        var error = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();

        if (!string.IsNullOrWhiteSpace(output))
            Console.WriteLine(output);
        if (!string.IsNullOrWhiteSpace(error))
            Console.Error.WriteLine(error);

        return proc.ExitCode == 0;
    }

    private static async Task AddProjectReferenceToHostAsync(string hostCsprojPath, string ns, string moduleName)
    {
        var content = await File.ReadAllTextAsync(hostCsprojPath);
        var moduleRef = $"../Modules/{ns}.Modules.{moduleName}/{ns}.Modules.{moduleName}.csproj";

        // Check if reference already exists
        if (content.Contains(moduleRef))
            return;

        // Find </Project> and insert ItemGroup with ProjectReference before it
        var insertPoint = content.LastIndexOf("</Project>", StringComparison.Ordinal);
        if (insertPoint < 0)
            return;

        var referenceBlock = $"""

          <ItemGroup>
            <ProjectReference Include="{moduleRef}" />
          </ItemGroup>

        """;

        var newContent = content.Insert(insertPoint, referenceBlock);
        await File.WriteAllTextAsync(hostCsprojPath, newContent);
    }

    private static async Task UpdateProgramCsAsync(string programPath, string ns, string moduleName)
    {
        var content = await File.ReadAllTextAsync(programPath);
        var moduleUsing = $"using {ns}.Modules.{moduleName};";
        var moduleRegistration = $"nac.AddModule<{moduleName}Module>();";

        // Add using if not present
        if (!content.Contains(moduleUsing))
        {
            // Insert using at top of file
            content = moduleUsing + Environment.NewLine + content;
        }

        // Check if module already registered
        if (content.Contains(moduleRegistration))
        {
            await File.WriteAllTextAsync(programPath, content);
            return;
        }

        // Find AddNacFramework block and add module registration
        // Look for pattern: nac => { ... }) or nac => \n{ ... })
        var nacBlockPattern = "nac =>";
        var nacIndex = content.IndexOf(nacBlockPattern, StringComparison.Ordinal);

        if (nacIndex >= 0)
        {
            // Find the opening brace after nac =>
            var braceIndex = content.IndexOf('{', nacIndex);
            if (braceIndex >= 0)
            {
                // Insert after the opening brace with proper indentation (4 spaces)
                var insertion = $"\n    {moduleRegistration}";
                content = content.Insert(braceIndex + 1, insertion);
            }
        }

        await File.WriteAllTextAsync(programPath, content);
    }

    private static async Task AddFeatureAsync(string path)
    {
        var (module, feature) = ParseModulePath(path);
        var (ns, _, _) = ReadManifest();
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
        var (ns, _, _) = ReadManifest();
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

    private static (string Namespace, string Name, string? LocalNacPath) ReadManifest()
    {
        var manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "nac.json");
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException("nac.json not found. Run 'nac new' first.");

        using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var solution = doc.RootElement.GetProperty("solution");
        var name = solution.GetProperty("name").GetString()!;
        var ns = solution.GetProperty("namespace").GetString() ?? name;
        string? localNacPath = null;
        if (doc.RootElement.TryGetProperty("localNacPath", out var localProp))
            localNacPath = localProp.GetString();
        return (ns, name, localNacPath);
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
