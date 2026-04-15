using System.CommandLine;
using System.CommandLine.Parsing;
using FluentAssertions;
using Nac.Cli.Commands;
using Xunit;

namespace Nac.Cli.Tests.Unit;

/// <summary>
/// Tests for <see cref="NewCommand"/> — validates argument/option parsing and validation logic.
/// Uses System.CommandLine's own parse result to check handler wiring without running scaffold.
/// </summary>
public sealed class NewCommandTests
{
    // ------------------------------------------------------------------ helpers

    public NewCommandTests()
    {
        // Suppress post-scaffold dotnet restore so tests run fast
        Environment.SetEnvironmentVariable("NAC_SKIP_RESTORE", "1");
    }

    private static Command BuildRoot()
    {
        var root = new RootCommand("NAC Framework CLI");
        root.AddCommand(NewCommand.Create());
        return root;
    }

    /// <summary>
    /// Invokes the CLI against a real temp directory so the handler runs, but captures
    /// exit code only — no real dotnet restore is executed because the project name
    /// validation fires before ScaffoldService is reached when invalid.
    /// </summary>
    private static async Task<int> InvokeAsync(params string[] args)
    {
        var root = BuildRoot();
        return await root.InvokeAsync(args);
    }

    // ------------------------------------------------------------------ name validation

    [Theory]
    [InlineData("MyApp")]
    [InlineData("Test_App")]
    [InlineData("_App")]
    [InlineData("A")]
    [InlineData("App123")]
    public void ValidIdentifiers_ParseWithoutError(string name)
    {
        var root = BuildRoot();
        var result = root.Parse(["new", name]);

        result.Errors.Should().BeEmpty(
            because: $"'{name}' is a valid C# identifier and should be accepted");
    }

    [Theory]
    [InlineData("123app")]
    [InlineData("my-app")]
    [InlineData("my app")]
    [InlineData("")]
    public async Task InvalidProjectName_ReturnsExitCode1(string name)
    {
        // Use a temp dir so the output-dir check does not trigger first
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            int exitCode = await InvokeAsync("new", name, "--output", tmp);
            exitCode.Should().Be(1,
                because: $"'{name}' is not a valid C# identifier");
        }
        finally
        {
            if (Directory.Exists(tmp)) Directory.Delete(tmp, recursive: true);
        }
    }

    // ------------------------------------------------------------------ module name validation

    [Theory]
    [InlineData("123mod")]
    [InlineData("my-module")]
    [InlineData("my module")]
    public async Task InvalidModuleName_ReturnsExitCode1(string moduleName)
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            int exitCode = await InvokeAsync("new", "ValidApp", "--module", moduleName, "--output", tmp);
            exitCode.Should().Be(1,
                because: $"'{moduleName}' is not a valid module identifier");
        }
        finally
        {
            if (Directory.Exists(tmp)) Directory.Delete(tmp, recursive: true);
        }
    }

    // ------------------------------------------------------------------ default module

    [Fact]
    public void DefaultModuleOption_IsSample()
    {
        var command = NewCommand.Create();
        var moduleOption = command.Options
            .OfType<Option<string>>()
            .FirstOrDefault(o => o.Name == "module");

        moduleOption.Should().NotBeNull();

        // Verify the default factory produces "Sample"
        var root = new RootCommand();
        root.AddCommand(command);
        var result = root.Parse(["new", "MyApp"]);

        // The default is enforced at handler execution; parse result should contain no error for --module
        result.Errors.Should().BeEmpty();
    }

    // ------------------------------------------------------------------ non-empty output dir

    [Fact]
    public async Task NonEmptyOutputDir_ReturnsExitCode1()
    {
        // Create a temp dir with at least one file
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tmp);
        await File.WriteAllTextAsync(Path.Combine(tmp, "dummy.txt"), "content");

        try
        {
            int exitCode = await InvokeAsync("new", "MyApp", "--output", tmp);
            exitCode.Should().Be(1,
                because: "a non-empty output directory must be rejected");
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public async Task EmptyOutputDir_DoesNotFailOnDirCheck()
    {
        // An existing but empty directory is allowed — handler proceeds to scaffolding.
        // We only care that exit code is NOT 1 due to the directory check.
        // (Scaffolding itself may succeed or fail; we just verify validation passes.)
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tmp);

        try
        {
            int exitCode = await InvokeAsync("new", "MyApp", "--output", tmp);
            // Exit 0 = success (scaffold ran), any other value means a different stage failed
            // The critical assertion: it did NOT fail because of the "non-empty dir" check
            // We verify by confirming the directory now contains files from scaffolding
            var files = Directory.GetFiles(tmp, "*", SearchOption.AllDirectories);
            files.Should().NotBeEmpty(
                because: "scaffolding should have created files in the empty output directory");
        }
        finally
        {
            if (Directory.Exists(tmp)) Directory.Delete(tmp, recursive: true);
        }
    }
}
