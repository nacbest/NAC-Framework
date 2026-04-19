using FluentAssertions;
using Nac.Identity.Permissions.Host;
using Xunit;

namespace Nac.Identity.Tests.Permissions;

/// <summary>
/// Contract test: ensures HostPermissions constants match the string literals used in
/// Nac.MultiTenancy.Management.Authorization.HostAdminOnlyFilter, which cannot reference
/// Nac.Identity directly. If a constant value changes, this test fails and alerts the
/// developer to update the corresponding literal in HostAdminOnlyFilter.cs.
/// </summary>
public class HostPermissionsContractTests
{
    [Fact]
    public void AccessAllTenants_HasExpectedStringValue()
    {
        // The literal "Host.AccessAllTenants" is hardcoded in HostAdminOnlyFilter.cs.
        // Changing this constant without updating that literal silently breaks host auth.
        HostPermissions.AccessAllTenants.Should().Be("Host.AccessAllTenants");
    }
}
