using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Nac.Core.Abstractions.Identity;

namespace Nac.Identity.Services;

/// <summary>
/// Reads the current authenticated user from the active <see cref="ClaimsPrincipal"/>
/// via <see cref="IHttpContextAccessor"/>. Registered as scoped.
/// </summary>
internal sealed class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    /// <inheritdoc/>
    public Guid Id => Guid.TryParse(
        User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    /// <inheritdoc/>
    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;

    /// <inheritdoc/>
    public string? Name => User?.FindFirst(ClaimTypes.Name)?.Value;

    /// <inheritdoc/>
    public string? TenantId
    {
        get
        {
            var v = User?.FindFirst(NacIdentityClaims.TenantId)?.Value;
            return string.IsNullOrEmpty(v) ? null : v;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<Guid> RoleIds
    {
        get
        {
            var raw = User?.FindFirst(NacIdentityClaims.RoleIds)?.Value;
            if (string.IsNullOrEmpty(raw)) return Array.Empty<Guid>();
            try { return JsonSerializer.Deserialize<Guid[]>(raw) ?? Array.Empty<Guid>(); }
            catch (JsonException) { return Array.Empty<Guid>(); }
        }
    }

    /// <inheritdoc/>
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc/>
    public bool IsHost => string.Equals(
        User?.FindFirst(NacIdentityClaims.IsHost)?.Value, "true", StringComparison.OrdinalIgnoreCase);
}
