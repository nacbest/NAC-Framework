using System.CommandLine;
using System.Text.RegularExpressions;
using Nac.Cli.Services;

namespace Nac.Cli.Commands;

/// <summary>
/// Defines the 'nac new' command for scaffolding NAC projects.
/// </summary>
public static partial class NewCommand
{
    // Only allow valid C# identifier characters (letters, digits, underscores)
    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex ValidIdentifierRegex();

    public static Command Create()
    {
        var nameArg = new Argument<string>("name", "Project name (PascalCase recommended)");
        var moduleOption = new Option<string>("--module", () => "Sample", "First module name");
        var outputOption = new Option<DirectoryInfo?>("--output", "Output directory (default: ./<name>)");
        var localOption = new Option<string?>("--local", "Path to local NAC framework source for development testing");

        var command = new Command("new", "Create a new NAC Framework project")
        {
            nameArg,
            moduleOption,
            outputOption,
            localOption
        };

        command.SetHandler(HandleAsync, nameArg, moduleOption, outputOption, localOption);
        return command;
    }

    private static async Task<int> HandleAsync(string name, string moduleName, DirectoryInfo? output, string? localPath)
    {
        if (!ValidIdentifierRegex().IsMatch(name))
        {
            Console.Error.WriteLine("Error: Project name must be a valid C# identifier (letters, digits, underscores).");
            return 1;
        }

        if (!ValidIdentifierRegex().IsMatch(moduleName))
        {
            Console.Error.WriteLine("Error: Module name must be a valid C# identifier (letters, digits, underscores).");
            return 1;
        }

        var outputDir = output?.FullName ?? Path.Combine(Directory.GetCurrentDirectory(), name);

        if (Directory.Exists(outputDir) && Directory.EnumerateFileSystemEntries(outputDir).Any())
        {
            Console.Error.WriteLine($"Error: Directory '{outputDir}' already exists and is not empty.");
            return 1;
        }

        var service = new ScaffoldService();
        return await service.ScaffoldAsync(name, moduleName, outputDir, localPath);
    }
}
