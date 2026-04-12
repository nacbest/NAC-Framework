using System.CommandLine;
using Nac.Cli.Templates;

namespace Nac.Cli.Commands;

/// <summary>
/// <c>nac new &lt;Name&gt;</c> — scaffolds a new NAC solution with Host project and nac.json manifest.
/// </summary>
internal static class NewCommand
{
    public static Command Create()
    {
        var nameArg = new Argument<string>("name") { Description = "Solution name" };
        var dbOption = new Option<string>("--db")
        {
            Description = "Database provider",
            DefaultValueFactory = _ => "postgresql",
        };

        var cmd = new Command("new", "Scaffold a new NAC solution") { nameArg, dbOption };
        cmd.SetAction(async (parseResult, ct) =>
        {
            var name = parseResult.GetValue(nameArg)!;
            var db = parseResult.GetValue(dbOption)!;
            await ExecuteAsync(name, db);
        });
        return cmd;
    }

    private static async Task ExecuteAsync(string name, string db)
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), name);

        if (Directory.Exists(root))
        {
            Console.Error.WriteLine($"Directory '{name}' already exists.");
            return;
        }

        var hostDir = Path.Combine(root, "src", $"{name}.Host");
        var modulesDir = Path.Combine(root, "src", "Modules");
        var testsDir = Path.Combine(root, "tests");

        Directory.CreateDirectory(hostDir);
        Directory.CreateDirectory(modulesDir);
        Directory.CreateDirectory(testsDir);

        await File.WriteAllTextAsync(Path.Combine(root, $"{name}.slnx"), CodeTemplates.SlnxFile(name));
        await File.WriteAllTextAsync(Path.Combine(root, "nac.json"), CodeTemplates.NacJson(name, db));
        await File.WriteAllTextAsync(Path.Combine(hostDir, $"{name}.Host.csproj"), CodeTemplates.HostCsproj(name));
        await File.WriteAllTextAsync(Path.Combine(hostDir, "Program.cs"), CodeTemplates.ProgramCs(name));
        await File.WriteAllTextAsync(Path.Combine(hostDir, "appsettings.json"), CodeTemplates.AppSettings(name));

        Console.WriteLine($"Created solution '{name}' at {root}");
        Console.WriteLine($"  src/{name}.Host/  — composition root");
        Console.WriteLine($"  src/Modules/     — add modules here");
        Console.WriteLine($"  nac.json         — framework manifest");
        Console.WriteLine();
        Console.WriteLine("Next: nac add module <ModuleName>");
    }
}
