using Nac.MultiTenancy.Management.Domain;
using Nac.MultiTenancy.Management.Dtos;

namespace Nac.MultiTenancy.Management.Services;

/// <summary>Static aggregate-to-DTO projection — kept simple, no AutoMapper.</summary>
internal static class TenantMapper
{
    public static TenantDto ToDto(Tenant t) => new(
        t.Id,
        t.Identifier,
        t.Name,
        t.IsolationMode,
        t.IsActive,
        new Dictionary<string, string?>(t.Properties, StringComparer.OrdinalIgnoreCase),
        t.CreatedAt,
        t.UpdatedAt);
}
