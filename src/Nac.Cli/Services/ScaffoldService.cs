using System.Diagnostics;
using System.Reflection;
using Scriban;

namespace Nac.Cli.Services;

/// <summary>
/// Loads embedded Scriban templates, renders with project model, writes output files.
/// </summary>
public sealed class ScaffoldService
{
    private const string TemplatePrefix = "Nac.Cli.Templates.nac_solution.";

    /// <summary>
    /// Reads the framework version from the CLI assembly instead of hardcoding it.
    /// The CLI shares Directory.Build.props with all NAC packages, so its version = framework version.
    /// </summary>
    private static string GetNacVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        // Strip build metadata (e.g. "2.0.0+abc123" → "2.0.0")
        if (infoVersion is not null)
        {
            var plusIndex = infoVersion.IndexOf('+');
            return plusIndex >= 0 ? infoVersion[..plusIndex] : infoVersion;
        }

        var version = assembly.GetName().Version;
        return version is not null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.0.0";
    }

    /// <summary>
    /// Template path → output path mapping using placeholder tokens.
    /// {Name} = project name, {Mod} = module name.
    /// </summary>
    private static readonly (string ResourceSuffix, string OutputPath)[] TemplateMappings =
    [
        ("slnx.sbn", "{Name}.slnx"),
        ("nac.json.sbn", "nac.json"),
        ("Directory.Build.props.sbn", "Directory.Build.props"),
        ("Directory.Packages.props.sbn", "Directory.Packages.props"),
        ("Host.Host.csproj.sbn", "src/{Name}.Host/{Name}.Host.csproj"),
        ("Host.Program.cstemplate", "src/{Name}.Host/Program.cs"),
        ("Host.appsettings.json.sbn", "src/{Name}.Host/appsettings.json"),
        ("Host.appsettings.Development.json.sbn", "src/{Name}.Host/appsettings.Development.json"),
        ("Shared.Shared.csproj.sbn", "src/{Name}.Shared/{Name}.Shared.csproj"),
        ("Module.Module.csproj.sbn", "src/Modules/{Name}.Modules.{Mod}/{Name}.Modules.{Mod}.csproj"),
        ("Module.ModuleClass.cstemplate", "src/Modules/{Name}.Modules.{Mod}/{Mod}Module.cs"),
        ("Module.Entity.cstemplate", "src/Modules/{Name}.Modules.{Mod}/Domain/Entities/{Mod}Item.cs"),
        ("Module.CreateCommand.cstemplate", "src/Modules/{Name}.Modules.{Mod}/Application/Commands/Create{Mod}Item/Create{Mod}ItemCommand.cs"),
        ("Module.CreateCommandHandler.cstemplate", "src/Modules/{Name}.Modules.{Mod}/Application/Commands/Create{Mod}Item/Create{Mod}ItemCommandHandler.cs"),
        ("Module.GetQuery.cstemplate", "src/Modules/{Name}.Modules.{Mod}/Application/Queries/Get{Mod}ItemById/Get{Mod}ItemByIdQuery.cs"),
        ("Module.GetQueryHandler.cstemplate", "src/Modules/{Name}.Modules.{Mod}/Application/Queries/Get{Mod}ItemById/Get{Mod}ItemByIdQueryHandler.cs"),
        ("Module.Endpoints.cstemplate", "src/Modules/{Name}.Modules.{Mod}/Endpoints/{Mod}ItemEndpoints.cs"),
        ("Module.DbContext.cstemplate", "src/Modules/{Name}.Modules.{Mod}/Infrastructure/{Mod}DbContext.cs"),
        ("Module.EntityConfiguration.cstemplate", "src/Modules/{Name}.Modules.{Mod}/Infrastructure/Configurations/{Mod}ItemConfiguration.cs"),
        ("Module.ServiceExtensions.cstemplate", "src/Modules/{Name}.Modules.{Mod}/Infrastructure/{Mod}ServiceCollectionExtensions.cs"),
        ("Tests.Tests.csproj.sbn", "tests/{Name}.Modules.{Mod}.Tests/{Name}.Modules.{Mod}.Tests.csproj"),
    ];

    /// <returns>0 on success, 1 on failure.</returns>
    public async Task<int> ScaffoldAsync(string projectName, string moduleName, string outputDir, string? localPath = null)
    {
        // Validate and resolve --local path
        if (localPath is not null)
        {
            localPath = Path.GetFullPath(localPath);
            var srcDir = Path.Combine(localPath, "src");
            if (!Directory.Exists(srcDir))
            {
                Console.Error.WriteLine($"Error: Local NAC path '{localPath}' does not contain 'src/' directory.");
                return 1;
            }

            var requiredProjects = new[] { "Nac.Core", "Nac.WebApi", "Nac.Persistence" };
            foreach (var proj in requiredProjects)
            {
                var csproj = Path.Combine(srcDir, proj, $"{proj}.csproj");
                if (!File.Exists(csproj))
                {
                    Console.Error.WriteLine($"Error: Missing required project: {csproj}");
                    return 1;
                }
            }

            // Normalize to forward slashes for cross-platform .csproj compatibility
            localPath = localPath.Replace(Path.DirectorySeparatorChar, '/');
            Console.WriteLine($"Using local NAC source: {localPath}");
        }

        Console.WriteLine($"Creating NAC project '{projectName}' with module '{moduleName}'...");
        Console.WriteLine();

        var assembly = Assembly.GetExecutingAssembly();
        var isLocal = localPath is not null;
        var model = new { project_name = projectName, module_name = moduleName, nac_version = GetNacVersion(), local_path = localPath ?? "", is_local = isLocal };
        var filesCreated = 0;
        var errors = 0;

        foreach (var (suffix, pathTemplate) in TemplateMappings)
        {
            var resourceName = TemplatePrefix + suffix;
            var templateContent = await ReadEmbeddedResourceAsync(assembly, resourceName);
            if (templateContent is null)
            {
                Console.Error.WriteLine($"  Error: Template not found — {suffix}");
                errors++;
                continue;
            }

            var template = Template.Parse(templateContent, resourceName);
            if (template.HasErrors)
            {
                Console.Error.WriteLine($"  Error parsing {suffix}: {string.Join(", ", template.Messages)}");
                errors++;
                continue;
            }

            var rendered = await template.RenderAsync(model);

            var relativePath = pathTemplate
                .Replace("{Name}", projectName)
                .Replace("{Mod}", moduleName);
            var fullPath = Path.Combine(outputDir, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllTextAsync(fullPath, rendered);
            filesCreated++;

            Console.WriteLine($"  Created: {relativePath}");
        }

        Console.WriteLine();
        Console.WriteLine($"Created {filesCreated} files.");

        if (errors > 0)
        {
            Console.Error.WriteLine($"{errors} template(s) failed. Generated project may be incomplete.");
            return 1;
        }

        Console.WriteLine();

        // Run dotnet restore (can be skipped via NAC_SKIP_RESTORE=1 for test environments)
        if (!string.Equals(Environment.GetEnvironmentVariable("NAC_SKIP_RESTORE"), "1", StringComparison.Ordinal))
            await RunDotnetRestoreAsync(outputDir);

        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine($"  cd {projectName}");
        Console.WriteLine("  dotnet build");
        Console.WriteLine("  dotnet run --project src/{0}.Host", projectName);

        return 0;
    }

    private static async Task RunDotnetRestoreAsync(string workingDirectory)
    {
        Console.WriteLine("Running dotnet restore...");
        try
        {
            var restore = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "restore",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            if (restore is null) return;

            await restore.WaitForExitAsync();
            if (restore.ExitCode != 0)
            {
                var error = await restore.StandardError.ReadToEndAsync();
                Console.Error.WriteLine($"dotnet restore failed: {error}");
            }
            else
            {
                Console.WriteLine("Restore completed successfully.");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Could not run dotnet restore: {ex.Message}");
        }
    }

    private static async Task<string?> ReadEmbeddedResourceAsync(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null) return null;
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
