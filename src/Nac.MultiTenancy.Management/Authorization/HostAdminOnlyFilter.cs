using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nac.Core.Abstractions.Identity;

namespace Nac.MultiTenancy.Management.Authorization;

/// <summary>
/// Action filter that rejects callers scoped to a specific tenant. Tenant
/// management is a host-level concern: only callers whose
/// <see cref="ICurrentUser.TenantId"/> is empty (i.e. the host realm) may proceed.
/// </summary>
internal sealed class HostAdminOnlyFilter : IAsyncActionFilter
{
    private readonly ICurrentUser _user;

    public HostAdminOnlyFilter(ICurrentUser user) => _user = user;

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!_user.IsAuthenticated || !string.IsNullOrEmpty(_user.TenantId))
        {
            context.Result = new ForbidResult();
            return;
        }
        await next();
    }
}
