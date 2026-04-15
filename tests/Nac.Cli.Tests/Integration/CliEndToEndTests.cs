using FluentAssertions;
using System.CommandLine;
using Nac.Cli.Commands;
using Xunit;

namespace Nac.Cli.Tests.Integration;

/// <summary>
/// End-to-end integration tests that invoke the CLI root command in-process
/// (same behaviour as running `nac new ...` via dotnet run) and assert on the
/// file-system output.  dotnet restore is run by ScaffoldService after scaffold;
/// we assert structure independently of whether restore succeeds.
/// </summary>
public sealed class CliEndToEndTests : IAsyncLifetime
{
    private string _tempBase = string.Empty;

    public Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable("NAC_SKIP_RESTORE", "1");
        _tempBase = Path.Combine(Path.GetTempPath(), $"nac-e2e-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempBase);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_tempBase))
            Directory.Delete(_tempBase, recursive: true);
        return Task.CompletedTask;
    }

    // ------------------------------------------------------------------ helpers

    private static RootCommand BuildRoot()
    {
        var root = new RootCommand("NAC Framework CLI");
        root.AddCommand(NewCommand.Create());
        return root;
    }

    private string UniqueOutputDir(string projectName)
        => Path.Combine(_tempBase, projectName);

    // ------------------------------------------------------------------ nac new TestApp

    [Fact]
    public async Task NacNew_DefaultArgs_CreatesOutputDirectory()
    {
        var outDir = UniqueOutputDir("TestApp");
        var root = BuildRoot();

        await root.InvokeAsync(["new", "TestApp", "--output", outDir]);

        Directory.Exists(outDir).Should().BeTrue(
            because: "CLI must create the output directory");
    }

    [Fact]
    public async Task NacNew_DefaultArgs_Creates22Files()
    {
        var outDir = UniqueOutputDir("TestApp");
        var root = BuildRoot();

        await root.InvokeAsync(["new", "TestApp", "--output", outDir]);

        var files = Directory.GetFiles(outDir, "*", SearchOption.AllDirectories);
        files.Should().HaveCount(21);
    }

    [Fact]
    public async Task NacNew_DefaultArgs_DefaultModuleIsSample()
    {
        var outDir = UniqueOutputDir("TestApp");
        var root = BuildRoot();

        int exitCode = await root.InvokeAsync(["new", "TestApp", "--output", outDir]);

        exitCode.Should().Be(0);

        // Default module = Sample — verify canonical Sample paths exist
        File.Exists(Path.Combine(outDir, "src", "Modules",
            "TestApp.Modules.Sample", "SampleModule.cs"))
            .Should().BeTrue(because: "default module name is 'Sample'");
    }

    [Fact]
    public async Task NacNew_DefaultArgs_SolutionFileContainsProjectName()
    {
        var outDir = UniqueOutputDir("TestApp");
        var root = BuildRoot();

        await root.InvokeAsync(["new", "TestApp", "--output", outDir]);

        var slnx = await File.ReadAllTextAsync(Path.Combine(outDir, "TestApp.slnx"));
        slnx.Should().Contain("TestApp");
    }

    // ------------------------------------------------------------------ nac new TestApp --module Catalog

    [Fact]
    public async Task NacNew_CustomModule_CreatesModuleStructure()
    {
        var outDir = UniqueOutputDir("TestApp2");
        var root = BuildRoot();

        int exitCode = await root.InvokeAsync(
            ["new", "TestApp2", "--module", "Catalog", "--output", outDir]);

        exitCode.Should().Be(0);

        Directory.Exists(
            Path.Combine(outDir, "src", "Modules", "TestApp2.Modules.Catalog"))
            .Should().BeTrue();

        // Infrastructure folder lives inside the module project (single-project pattern)
        Directory.Exists(
            Path.Combine(outDir, "src", "Modules", "TestApp2.Modules.Catalog", "Infrastructure"))
            .Should().BeTrue();
    }

    [Fact]
    public async Task NacNew_CustomModule_NoSamplePathsExist()
    {
        var outDir = UniqueOutputDir("TestApp3");
        var root = BuildRoot();

        await root.InvokeAsync(
            ["new", "TestApp3", "--module", "Catalog", "--output", outDir]);

        // When a custom module is specified, no "Sample" module path should exist
        Directory.Exists(
            Path.Combine(outDir, "src", "Modules", "TestApp3.Modules.Sample"))
            .Should().BeFalse(because: "module name was overridden to 'Catalog'");
    }

    [Fact]
    public async Task NacNew_CustomModule_SolutionFileContainsModuleName()
    {
        var outDir = UniqueOutputDir("TestApp4");
        var root = BuildRoot();

        await root.InvokeAsync(
            ["new", "TestApp4", "--module", "Catalog", "--output", outDir]);

        var slnx = await File.ReadAllTextAsync(Path.Combine(outDir, "TestApp4.slnx"));
        slnx.Should().Contain("Catalog");
        slnx.Should().NotContain("Sample");
    }

    // ------------------------------------------------------------------ nac new TestApp --output /tmp/custom

    [Fact]
    public async Task NacNew_CustomOutputDir_CreatesFilesInSpecifiedDir()
    {
        var customOutput = Path.Combine(_tempBase, "custom-output-dir");
        var root = BuildRoot();

        int exitCode = await root.InvokeAsync(
            ["new", "MyProject", "--output", customOutput]);

        exitCode.Should().Be(0);
        Directory.Exists(customOutput).Should().BeTrue();

        var files = Directory.GetFiles(customOutput, "*", SearchOption.AllDirectories);
        files.Should().HaveCount(21);
    }

    [Fact]
    public async Task NacNew_CustomOutputDir_FilesUseProjectNameNotDirName()
    {
        // Even when --output differs from the project name, file/folder names
        // inside are derived from the project name argument, not the dir name.
        var customOutput = Path.Combine(_tempBase, "different-folder-name");
        var root = BuildRoot();

        await root.InvokeAsync(
            ["new", "ActualProject", "--output", customOutput]);

        File.Exists(Path.Combine(customOutput, "ActualProject.slnx")).Should().BeTrue();
        Directory.Exists(Path.Combine(customOutput, "src", "ActualProject.Host"))
            .Should().BeTrue();
    }

    // ------------------------------------------------------------------ error cases

    [Fact]
    public async Task NacNew_ExistingNonEmptyOutputDir_ReturnsExitCode1()
    {
        var outDir = UniqueOutputDir("ExistingApp");
        Directory.CreateDirectory(outDir);
        await File.WriteAllTextAsync(Path.Combine(outDir, "something.txt"), "content");

        var root = BuildRoot();
        int exitCode = await root.InvokeAsync(["new", "ExistingApp", "--output", outDir]);

        exitCode.Should().Be(1);
    }

    [Fact]
    public async Task NacNew_InvalidProjectName_ReturnsExitCode1AndCreatesNoFiles()
    {
        var outDir = UniqueOutputDir("invalid-output");
        var root = BuildRoot();

        int exitCode = await root.InvokeAsync(["new", "123-invalid", "--output", outDir]);

        exitCode.Should().Be(1);
        // Output directory should NOT have been created (validation failed before scaffolding)
        Directory.Exists(outDir).Should().BeFalse();
    }
}
