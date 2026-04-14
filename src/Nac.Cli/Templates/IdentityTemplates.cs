namespace Nac.Cli.Templates;

/// <summary>
/// Code snippets for installing Nac.Identity into a Host project.
/// Uses raw string literals consistent with CodeTemplates.cs patterns.
/// </summary>
internal static class IdentityTemplates
{
    /// <summary>
    /// Using statements to prepend to Program.cs.
    /// </summary>
    public static string ProgramCsUsings() =>
        """
        using Microsoft.EntityFrameworkCore;
        using Nac.Identity.Extensions;
        """;

    /// <summary>
    /// Service registration block to inject before builder.Build().
    /// </summary>
    public static string AddNacIdentityServices() =>
        """
        // NAC Identity
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddNacIdentity(builder.Configuration, db => db.UseNpgsql(connectionString));
        """;

    /// <summary>
    /// Middleware block to inject after app = builder.Build().
    /// </summary>
    public static string UseNacIdentityMiddleware() =>
        """
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseNacIdentity(seedRoles: true);
        """;

    /// <summary>
    /// NacIdentity JSON config section (object only, not wrapped in {}).
    /// Caller merges this into the root appsettings.json object.
    /// </summary>
    public static string AppSettingsNacIdentitySection(string ns) => $$"""
        {
          "NacIdentity": {
            "SigningKey": "CHANGE-THIS-IN-PRODUCTION-MIN-32-CHARS-REQUIRED",
            "Issuer": "{{ns}}",
            "Audience": "{{ns}}",
            "AccessTokenExpiry": "00:15:00",
            "RefreshTokenExpiry": "7.00:00:00"
          }
        }
        """;

    /// <summary>
    /// NuGet package references XML block for Host.csproj.
    /// </summary>
    public static string CsprojPackageReference() =>
        """
          <ItemGroup>
            <PackageReference Include="Nac.Identity" Version="1.0.0" />
            <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.4" />
          </ItemGroup>
        """;

    /// <summary>
    /// Local project references XML block for Host.csproj (dev mode with localNacPath).
    /// </summary>
    public static string CsprojProjectReference(string localNacPath) =>
        $"""
          <ItemGroup>
            <ProjectReference Include="{localNacPath}/src/Nac.Identity/Nac.Identity.csproj" />
            <ProjectReference Include="{localNacPath}/src/Nac.Persistence.PostgreSQL/Nac.Persistence.PostgreSQL.csproj" />
          </ItemGroup>
        """;
}
