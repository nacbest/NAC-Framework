using System.Security.Claims;
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
    public string TenantId => User?.FindFirst(NacIdentityClaims.TenantId)?.Value ?? string.Empty;

    /// <inheritdoc/>
    public IReadOnlyList<string> Roles => User?.FindAll(ClaimTypes.Role)
        .Select(c => c.Value).ToList() ?? [];

    /// <inheritdoc/>
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
