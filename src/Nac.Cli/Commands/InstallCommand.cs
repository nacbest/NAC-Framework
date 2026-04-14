using System.CommandLine;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Nac.Cli.Templates;

namespace Nac.Cli.Commands;

/// <summary>
/// <c>nac install skill|identity</c> — installs Claude skills or framework components to local project.
/// </summary>
internal static class InstallCommand
{
    public static Command Create()
    {
        var cmd = new Command("install", "Install components to local project");
        cmd.Add(CreateSkillCommand());
        cmd.Add(CreateIdentityCommand());
        return cmd;
    }

    // -------------------------------------------------------------------------
    // nac install skill <name>
    // -------------------------------------------------------------------------

    private static Command CreateSkillCommand()
    {
        var nameArg = new Argument<string>("name") { Description = "Skill name (e.g., identity)" };
        var cmd = new Command("skill", "Install a Claude skill to .claude/skills/") { nameArg };
        cmd.SetAction(async (parseResult, ct) =>
        {
            var name = parseResult.GetValue(nameArg)!;
            await InstallSkillAsync(name);
        });
        return cmd;
    }

    private static async Task InstallSkillAsync(string name)
    {
        var skillName = name.ToLowerInvariant();

        if (skillName != "identity")
        {
            Console.Error.WriteLine($"Unknown skill '{name}'. Available: identity");
            return;
        }

        var skillDir = Path.Combine(".claude", "skills", "nac-identity");
        var refsDir = Path.Combine(skillDir, "references");

        // Create directories
        Directory.CreateDirectory(refsDir);

        // Write skill files
        await File.WriteAllTextAsync(
            Path.Combine(skillDir, "SKILL.md"),
            SkillTemplates.NacIdentitySkillMd());

        await File.WriteAllTextAsync(
            Path.Combine(refsDir, "auth-endpoints.md"),
            SkillTemplates.AuthEndpointsMd());

        await File.WriteAllTextAsync(
            Path.Combine(refsDir, "tenant-flows.md"),
            SkillTemplates.TenantFlowsMd());

        await File.WriteAllTextAsync(
            Path.Combine(refsDir, "migration-safety.md"),
            SkillTemplates.MigrationSafetyMd());

        Console.WriteLine($"Installed skill 'nac-identity' to {skillDir}");
        Console.WriteLine();
        Console.WriteLine("Files created:");
        Console.WriteLine("  SKILL.md                       — Main workflow");
        Console.WriteLine("  references/auth-endpoints.md   — 8 endpoint patterns");
        Console.WriteLine("  references/tenant-flows.md     — Multi-tenancy flows");
        Console.WriteLine("  references/migration-safety.md — Migration protocol");
        Console.WriteLine();
        Console.WriteLine("Usage: /nac-identity");
    }

    // -------------------------------------------------------------------------
    // nac install identity
    // -------------------------------------------------------------------------

    private static Command CreateIdentityCommand()
    {
        var cmd = new Command("identity", "Install Nac.Identity to Host project");
        cmd.SetAction(async (parseResult, ct) =>
        {
            await InstallIdentityAsync();
        });
        return cmd;
    }

    private static async Task InstallIdentityAsync()
    {
        // 1. Read manifest
        var (ns, _, localNacPath) = ReadManifest();

        // 2. Locate Host project files
        var hostDir = Path.Combine("src", $"{ns}.Host");
        var hostCsproj = Path.Combine(hostDir, $"{ns}.Host.csproj");
        var programCs = Path.Combine(hostDir, "Program.cs");
        var appSettings = Path.Combine(hostDir, "appsettings.json");

        if (!File.Exists(hostCsproj))
        {
            Console.Error.WriteLine($"Host project not found at {hostCsproj}");
            Console.Error.WriteLine("Run 'nac new' first to scaffold the solution.");
            return;
        }

        // 3. Idempotency: skip if already installed
        if (File.Exists(programCs))
        {
            var programContent = await File.ReadAllTextAsync(programCs);
            if (programContent.Contains("AddNacIdentity"))
            {
                Console.WriteLine("Nac.Identity is already installed.");
                return;
            }
        }

        // 4. Add package/project references to Host.csproj
        await AddIdentityReferencesAsync(hostCsproj, localNacPath);

        // 5. Update Program.cs
        if (File.Exists(programCs))
            await UpdateProgramCsForIdentityAsync(programCs);

        // 6. Update appsettings.json
        if (File.Exists(appSettings))
            await UpdateAppSettingsAsync(appSettings, ns);

        // 7. Verify build
        Console.WriteLine();
        Console.WriteLine("Verifying build...");
        var buildSuccess = await RunDotnetAsync("build -v q");

        // 8. Summary
        Console.WriteLine();
        Console.WriteLine("Nac.Identity installed:");
        Console.WriteLine($"  {hostCsproj} — package references added");
        Console.WriteLine($"  {programCs} — identity services registered");
        Console.WriteLine($"  {appSettings} — NacIdentity config added");

        if (buildSuccess)
            Console.WriteLine("  Build verified");
        else
            Console.WriteLine("  Build failed — check errors above");

        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine("  1. Update appsettings.json: replace the placeholder SigningKey");
        Console.WriteLine($"  2. Add migration: dotnet ef migrations add InitialIdentity -p src/Nac.Identity -s src/{ns}.Host");
        Console.WriteLine("  3. Install skill: nac install skill identity");
    }

    /// <summary>
    /// Appends Nac.Identity package or project references to Host.csproj.
    /// Skips if the reference already exists (idempotent).
    /// </summary>
    private static async Task AddIdentityReferencesAsync(string hostCsproj, string? localNacPath)
    {
        var content = await File.ReadAllTextAsync(hostCsproj);

        // Determine the sentinel string for idempotency check
        var sentinel = localNacPath != null
            ? "Nac.Identity/Nac.Identity.csproj"
            : "Nac.Identity\"";

        if (content.Contains(sentinel))
            return;

        var referenceBlock = localNacPath != null
            ? IdentityTemplates.CsprojProjectReference(localNacPath)
            : IdentityTemplates.CsprojPackageReference();

        // Insert before </Project>
        var insertPoint = content.LastIndexOf("</Project>", StringComparison.Ordinal);
        if (insertPoint < 0)
        {
            Console.Error.WriteLine("Could not find </Project> in Host.csproj — skipping reference injection.");
            return;
        }

        var newContent = content.Insert(insertPoint, $"\n{referenceBlock}\n");
        await File.WriteAllTextAsync(hostCsproj, newContent);
    }

    /// <summary>
    /// Injects using statements, AddNacIdentity() call, and UseNacIdentity() middleware
    /// into an existing Program.cs. Insertion is position-aware and idempotent per snippet.
    /// </summary>
    private static async Task UpdateProgramCsForIdentityAsync(string programCs)
    {
        var content = await File.ReadAllTextAsync(programCs);

        // --- Using statements (prepend to top if not present) ---
        foreach (var usingLine in new[] { "using Nac.Identity.Extensions;", "using Microsoft.EntityFrameworkCore;" })
        {
            if (!content.Contains(usingLine))
                content = usingLine + Environment.NewLine + content;
        }

        // --- AddNacIdentity services (insert before builder.Build()) ---
        const string buildCall = "var app = builder.Build();";
        if (!content.Contains("AddNacIdentity") && content.Contains(buildCall))
        {
            var servicesBlock = Environment.NewLine
                + IdentityTemplates.AddNacIdentityServices()
                + Environment.NewLine;

            var buildIndex = content.IndexOf(buildCall, StringComparison.Ordinal);
            content = content.Insert(buildIndex, servicesBlock + Environment.NewLine);
        }

        // --- UseNacIdentity middleware (insert before app.Run()) ---
        const string runCall = "app.Run();";
        if (!content.Contains("UseNacIdentity") && content.Contains(runCall))
        {
            var middlewareBlock = Environment.NewLine
                + IdentityTemplates.UseNacIdentityMiddleware()
                + Environment.NewLine;

            var runIndex = content.IndexOf(runCall, StringComparison.Ordinal);
            content = content.Insert(runIndex, middlewareBlock + Environment.NewLine);
        }

        await File.WriteAllTextAsync(programCs, content);
    }

    /// <summary>
    /// Merges NacIdentity section into appsettings.json using JSON parse + serialize.
    /// Idempotent: skips if section already present.
    /// </summary>
    private static async Task UpdateAppSettingsAsync(string appSettings, string ns)
    {
        var rawJson = await File.ReadAllTextAsync(appSettings);
        var root = JsonNode.Parse(rawJson)?.AsObject();

        if (root is null)
        {
            Console.Error.WriteLine("Could not parse appsettings.json — skipping config update.");
            return;
        }

        // Idempotency: skip if section already present
        if (root.ContainsKey("NacIdentity"))
            return;

        // Build the NacIdentity section
        var identitySection = JsonNode.Parse($$"""
            {
                "SigningKey": "CHANGE-THIS-IN-PRODUCTION-MIN-32-CHARS-REQUIRED",
                "Issuer": "{{ns}}",
                "Audience": "{{ns}}",
                "AccessTokenExpiry": "00:15:00",
                "RefreshTokenExpiry": "7.00:00:00"
            }
            """);

        root["NacIdentity"] = identitySection;

        var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(appSettings, root.ToJsonString(options));
    }

    // -------------------------------------------------------------------------
    // Shared helpers (mirrors AddCommand patterns)
    // -------------------------------------------------------------------------

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
}
