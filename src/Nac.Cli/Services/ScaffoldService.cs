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
    private const string NacVersion = "1.0.2";

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
        ("Module.Core.Module.csproj.sbn", "src/Modules/{Name}.Modules.{Mod}/{Name}.Modules.{Mod}.csproj"),
        ("Module.Core.ModuleClass.cstemplate", "src/Modules/{Name}.Modules.{Mod}/{Mod}Module.cs"),
        ("Module.Core.Entity.cstemplate", "src/Modules/{Name}.Modules.{Mod}/Domain/Entities/{Mod}Item.cs"),
        ("Module.Core.CreateCommand.cstemplate", "src/Modules/{Name}.Modules.{Mod}/Application/Commands/Create{Mod}Item/Create{Mod}ItemCommand.cs"),
        ("Module.Core.CreateCommandHandler.cstemplate", "src/Modules/{Name}.Modules.{Mod}/Application/Commands/Create{Mod}Item/Create{Mod}ItemCommandHandler.cs"),
        ("Module.Core.GetQuery.cstemplate", "src/Modules/{Name}.Modules.{Mod}/Application/Queries/Get{Mod}ItemById/Get{Mod}ItemByIdQuery.cs"),
        ("Module.Core.GetQueryHandler.cstemplate", "src/Modules/{Name}.Modules.{Mod}/Application/Queries/Get{Mod}ItemById/Get{Mod}ItemByIdQueryHandler.cs"),
        ("Module.Core.Endpoints.cstemplate", "src/Modules/{Name}.Modules.{Mod}/Endpoints/{Mod}ItemEndpoints.cs"),
        ("Module.Infrastructure.ModuleInfra.csproj.sbn", "src/Modules/{Name}.Modules.{Mod}.Infrastructure/{Name}.Modules.{Mod}.Infrastructure.csproj"),
        ("Module.Infrastructure.DbContext.cstemplate", "src/Modules/{Name}.Modules.{Mod}.Infrastructure/{Mod}DbContext.cs"),
        ("Module.Infrastructure.EntityConfiguration.cstemplate", "src/Modules/{Name}.Modules.{Mod}.Infrastructure/Configurations/{Mod}ItemConfiguration.cs"),
        ("Module.Infrastructure.InfraExtensions.cstemplate", "src/Modules/{Name}.Modules.{Mod}.Infrastructure/{Mod}InfrastructureExtensions.cs"),
        ("Tests.Tests.csproj.sbn", "tests/{Name}.Modules.{Mod}.Tests/{Name}.Modules.{Mod}.Tests.csproj"),
    ];

    /// <returns>0 on success, 1 on failure.</returns>
    public async Task<int> ScaffoldAsync(string projectName, string moduleName, string outputDir)
    {
        Console.WriteLine($"Creating NAC project '{projectName}' with module '{moduleName}'...");
        Console.WriteLine();

        var assembly = Assembly.GetExecutingAssembly();
        var model = new { project_name = projectName, module_name = moduleName, nac_version = NacVersion };
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

        // Run dotnet restore
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
