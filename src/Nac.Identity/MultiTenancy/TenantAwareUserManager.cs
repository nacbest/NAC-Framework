using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nac.Core.MultiTenancy;
using Nac.Identity.Entities;

namespace Nac.Identity.MultiTenancy;

/// <summary>
/// UserManager that automatically stamps the current tenant onto new users.
/// Scopes CreateAsync operations by injecting the active TenantId from <see cref="ITenantContext"/>.
/// </summary>
public class TenantAwareUserManager<TUser> : UserManager<TUser>
    where TUser : NacIdentityUser
{
    private readonly ITenantContext _tenantContext;

    public TenantAwareUserManager(
        IUserStore<TUser> store,
        IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<TUser> passwordHasher,
        IEnumerable<IUserValidator<TUser>> userValidators,
        IEnumerable<IPasswordValidator<TUser>> passwordValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        IServiceProvider services,
        ILogger<TenantAwareUserManager<TUser>> logger,
        ITenantContext tenantContext)
        : base(store, optionsAccessor, passwordHasher, userValidators,
               passwordValidators, keyNormalizer, errors, services, logger)
    {
        _tenantContext = tenantContext;
    }

    /// <inheritdoc/>
    public override async Task<IdentityResult> CreateAsync(TUser user, string password)
    {
        StampTenantId(user);
        return await base.CreateAsync(user, password);
    }

    /// <inheritdoc/>
    public override async Task<IdentityResult> CreateAsync(TUser user)
    {
        StampTenantId(user);
        return await base.CreateAsync(user);
    }

    /// <summary>
    /// Stamps TenantId onto the user if the tenant context is active and the user has no TenantId yet.
    /// </summary>
    private void StampTenantId(TUser user)
    {
        if (_tenantContext.TenantId is not null && string.IsNullOrEmpty(user.TenantId))
            user.TenantId = _tenantContext.TenantId;
    }
}
