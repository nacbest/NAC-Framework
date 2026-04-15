using System.Reflection;
using FluentAssertions;
using Nac.Cli.Services;
using Xunit;

namespace Nac.Cli.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="ScaffoldService"/>.
/// Each test scaffolds into an isolated temp directory and cleans up after itself.
/// NAC_SKIP_RESTORE=1 suppresses the post-scaffold dotnet restore so tests run fast.
/// </summary>
public sealed class ScaffoldServiceTests : IAsyncLifetime
{
    private string _outputDir = string.Empty;

    public Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable("NAC_SKIP_RESTORE", "1");
        _outputDir = Path.Combine(Path.GetTempPath(), $"nac-test-{Guid.NewGuid():N}");
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_outputDir))
            Directory.Delete(_outputDir, recursive: true);
        return Task.CompletedTask;
    }

    // ------------------------------------------------------------------ expected paths

    /// <summary>
    /// All 22 output paths expected for project "MyApp" / module "Sample".
    /// Mirrors <see cref="ScaffoldService"/> TemplateMappings with {Name}=MyApp, {Mod}=Sample.
    /// </summary>
    private static readonly string[] ExpectedRelativePaths =
    [
        "MyApp.slnx",
        "nac.json",
        "Directory.Build.props",
        "Directory.Packages.props",
        "src/MyApp.Host/MyApp.Host.csproj",
        "src/MyApp.Host/Program.cs",
        "src/MyApp.Host/appsettings.json",
        "src/MyApp.Host/appsettings.Development.json",
        "src/MyApp.Shared/MyApp.Shared.csproj",
        "src/Modules/MyApp.Modules.Sample/MyApp.Modules.Sample.csproj",
        "src/Modules/MyApp.Modules.Sample/SampleModule.cs",
        "src/Modules/MyApp.Modules.Sample/Domain/Entities/SampleItem.cs",
        "src/Modules/MyApp.Modules.Sample/Application/Commands/CreateSampleItem/CreateSampleItemCommand.cs",
        "src/Modules/MyApp.Modules.Sample/Application/Commands/CreateSampleItem/CreateSampleItemCommandHandler.cs",
        "src/Modules/MyApp.Modules.Sample/Application/Queries/GetSampleItemById/GetSampleItemByIdQuery.cs",
        "src/Modules/MyApp.Modules.Sample/Application/Queries/GetSampleItemById/GetSampleItemByIdQueryHandler.cs",
        "src/Modules/MyApp.Modules.Sample/Endpoints/SampleItemEndpoints.cs",
        "src/Modules/MyApp.Modules.Sample.Infrastructure/MyApp.Modules.Sample.Infrastructure.csproj",
        "src/Modules/MyApp.Modules.Sample.Infrastructure/SampleDbContext.cs",
        "src/Modules/MyApp.Modules.Sample.Infrastructure/Configurations/SampleItemConfiguration.cs",
        "src/Modules/MyApp.Modules.Sample.Infrastructure/SampleInfrastructureExtensions.cs",
        "tests/MyApp.Modules.Sample.Tests/MyApp.Modules.Sample.Tests.csproj",
    ];

    // ------------------------------------------------------------------ file count

    [Fact]
    public async Task ScaffoldAsync_Creates22Files()
    {
        var service = new ScaffoldService();

        await service.ScaffoldAsync("MyApp", "Sample", _outputDir);

        var allFiles = Directory.GetFiles(_outputDir, "*", SearchOption.AllDirectories);
        allFiles.Should().HaveCount(22,
            because: "22 templates are mapped in ScaffoldService");
    }

    [Fact]
    public async Task ScaffoldAsync_ReturnsZeroOnSuccess()
    {
        var service = new ScaffoldService();

        var result = await service.ScaffoldAsync("MyApp", "Sample", _outputDir);

        result.Should().Be(0);
    }

    // ------------------------------------------------------------------ directory structure

    [Fact]
    public async Task ScaffoldAsync_CreatesExpectedDirectoryStructure()
    {
        var service = new ScaffoldService();
        await service.ScaffoldAsync("MyApp", "Sample", _outputDir);

        var expectedDirs = new[]
        {
            Path.Combine(_outputDir, "src", "MyApp.Host"),
            Path.Combine(_outputDir, "src", "MyApp.Shared"),
            Path.Combine(_outputDir, "src", "Modules", "MyApp.Modules.Sample"),
            Path.Combine(_outputDir, "src", "Modules", "MyApp.Modules.Sample.Infrastructure"),
            Path.Combine(_outputDir, "tests", "MyApp.Modules.Sample.Tests"),
        };

        foreach (var dir in expectedDirs)
        {
            Directory.Exists(dir).Should().BeTrue(
                because: $"directory '{dir}' should have been created by scaffolding");
        }
    }

    // ------------------------------------------------------------------ all expected files

    [Fact]
    public async Task ScaffoldAsync_CreatesAllExpectedFiles()
    {
        var service = new ScaffoldService();
        await service.ScaffoldAsync("MyApp", "Sample", _outputDir);

        foreach (var relativePath in ExpectedRelativePaths)
        {
            var fullPath = Path.Combine(_outputDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
            File.Exists(fullPath).Should().BeTrue(
                because: $"file '{relativePath}' should have been scaffolded");
        }
    }

    // ------------------------------------------------------------------ token replacement in file paths

    [Fact]
    public async Task ScaffoldAsync_ReplacesNameTokenInPaths()
    {
        var service = new ScaffoldService();
        await service.ScaffoldAsync("Acme", "Inventory", _outputDir);

        // Spot-check: solution file should use project name
        File.Exists(Path.Combine(_outputDir, "Acme.slnx")).Should().BeTrue();

        // Host project dir should use project name
        Directory.Exists(Path.Combine(_outputDir, "src", "Acme.Host")).Should().BeTrue();
    }

    [Fact]
    public async Task ScaffoldAsync_ReplacesModTokenInPaths()
    {
        var service = new ScaffoldService();
        await service.ScaffoldAsync("Acme", "Inventory", _outputDir);

        // Module core dir should include module name
        Directory.Exists(
            Path.Combine(_outputDir, "src", "Modules", "Acme.Modules.Inventory"))
            .Should().BeTrue();

        // Module infrastructure dir should include module name
        Directory.Exists(
            Path.Combine(_outputDir, "src", "Modules", "Acme.Modules.Inventory.Infrastructure"))
            .Should().BeTrue();
    }

    // ------------------------------------------------------------------ template rendering (content)

    [Fact]
    public async Task ScaffoldAsync_RendersProjectNameInContent()
    {
        var service = new ScaffoldService();
        await service.ScaffoldAsync("Acme", "Inventory", _outputDir);

        var slnxContent = await File.ReadAllTextAsync(Path.Combine(_outputDir, "Acme.slnx"));
        slnxContent.Should().Contain("Acme",
            because: "solution file content should reference the project name");
        slnxContent.Should().NotContain("{{ project_name }}",
            because: "Scriban tokens should be fully rendered");
    }

    [Fact]
    public async Task ScaffoldAsync_RendersModuleNameInContent()
    {
        var service = new ScaffoldService();
        await service.ScaffoldAsync("Acme", "Inventory", _outputDir);

        var slnxContent = await File.ReadAllTextAsync(Path.Combine(_outputDir, "Acme.slnx"));
        slnxContent.Should().Contain("Inventory",
            because: "solution file should reference the module name");
        slnxContent.Should().NotContain("{{ module_name }}",
            because: "Scriban tokens should be fully rendered");
    }

    [Fact]
    public async Task ScaffoldAsync_RendersNacVersionInPackagesProps()
    {
        var service = new ScaffoldService();
        await service.ScaffoldAsync("Acme", "Inventory", _outputDir);

        // nac_version is rendered into Directory.Packages.props (central version management),
        // NOT into individual csproj files — those reference packages without a Version attribute.
        var packagesProps = await File.ReadAllTextAsync(
            Path.Combine(_outputDir, "Directory.Packages.props"));

        packagesProps.Should().NotContain("{{ nac_version }}",
            because: "version token should be fully rendered");

        // Version is read dynamically from the CLI assembly, so assert it matches
        var assembly = typeof(ScaffoldService).Assembly;
        var infoVersion = assembly.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var expectedVersion = infoVersion is not null
            ? (infoVersion.IndexOf('+') is var i and >= 0 ? infoVersion[..i] : infoVersion)
            : assembly.GetName().Version is { } v ? $"{v.Major}.{v.Minor}.{v.Build}" : "0.0.0";

        packagesProps.Should().Contain(expectedVersion,
            because: "rendered Directory.Packages.props should embed the NAC version from assembly");
    }

    [Fact]
    public async Task ScaffoldAsync_CstemplateLiteralsNotRenderedAsScriban()
    {
        // .cstemplate files intentionally use {{ }} for C# interpolation placeholders
        // that look like Scriban but are NOT — Scriban should not corrupt them.
        // Verify the Program.cs output actually contains the project/module names
        // (rendered via Scriban from .cstemplate) rather than raw tokens.
        var service = new ScaffoldService();
        await service.ScaffoldAsync("Acme", "Inventory", _outputDir);

        var programCs = await File.ReadAllTextAsync(
            Path.Combine(_outputDir, "src", "Acme.Host", "Program.cs"));

        programCs.Should().Contain("Acme",
            because: "Program.cs should reference the project name");
        programCs.Should().Contain("Inventory",
            because: "Program.cs should reference the module name");
    }

    // ------------------------------------------------------------------ custom module name

    [Fact]
    public async Task ScaffoldAsync_CustomModuleName_UsedThroughout()
    {
        var service = new ScaffoldService();
        await service.ScaffoldAsync("MyApp", "Catalog", _outputDir);

        // File produced with custom module
        File.Exists(Path.Combine(_outputDir,
            "src", "Modules", "MyApp.Modules.Catalog", "CatalogModule.cs"))
            .Should().BeTrue();

        // Tests project uses custom module
        File.Exists(Path.Combine(_outputDir,
            "tests", "MyApp.Modules.Catalog.Tests", "MyApp.Modules.Catalog.Tests.csproj"))
            .Should().BeTrue();
    }
}
