using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Nac.Core.Abstractions.Identity;
using Nac.Core.Abstractions.Permissions;
using Nac.Identity.Authentication;
using Nac.Identity.Context;
using Nac.Identity.Impersonation;
using Nac.Identity.Jwt;
using Nac.Identity.Memberships;
using Nac.Identity.Permissions;
using Nac.Identity.Permissions.Cache;
using Nac.Identity.Permissions.Grants;
using Nac.Identity.Permissions.Host;
using Nac.Identity.Roles;
using Nac.Identity.Services;
using Nac.Identity.Users;

namespace Nac.Identity.Extensions;

/// <summary>
/// Extension methods for registering NAC Identity services into the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers ASP.NET Core Identity, JWT Bearer authentication, and NAC-specific
    /// identity services (<see cref="ICurrentUser"/>, <see cref="IIdentityService"/>,
    /// <see cref="JwtTokenService"/>) into the service collection.
    /// </summary>
    /// <typeparam name="TContext">
    /// Concrete <see cref="NacIdentityDbContext"/> that provides the Identity store.
    /// </typeparam>
    /// <param name="services">The application service collection.</param>
    /// <param name="configure">Delegate to configure <see cref="NacIdentityOptions"/>.</param>
    public static IServiceCollection AddNacIdentity<TContext>(
        this IServiceCollection services,
        Action<NacIdentityOptions> configure)
        where TContext : NacIdentityDbContext
    {
        var options = new NacIdentityOptions();
        configure(options);

        if (string.IsNullOrWhiteSpace(options.Jwt.SecretKey) || options.Jwt.SecretKey.Length < 32)
            throw new InvalidOperationException(
                "JwtOptions.SecretKey must be at least 32 characters for HMAC-SHA256.");

        // ── ASP.NET Core Identity ─────────────────────────────────────────────

        services.AddIdentityCore<NacUser>(opts => options.ConfigureIdentity?.Invoke(opts))
            .AddRoles<NacRole>()
            .AddEntityFrameworkStores<TContext>()
            .AddDefaultTokenProviders();

        // ── JWT options snapshot ──────────────────────────────────────────────

        services.Configure<JwtOptions>(j =>
        {
            j.SecretKey = options.Jwt.SecretKey;
            j.Issuer = options.Jwt.Issuer;
            j.Audience = options.Jwt.Audience;
            j.ExpirationMinutes = options.Jwt.ExpirationMinutes;
        });

        // ── JWT Bearer authentication ─────────────────────────────────────────

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = options.Jwt.Issuer,
                    ValidAudience = options.Jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(options.Jwt.SecretKey)),
                };
                jwt.Events = new JwtBearerEvents
                {
                    OnTokenValidated = JtiRevocationValidator.ValidateAsync,
                };
            });

        // ── NAC services ──────────────────────────────────────────────────────

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserAccessor>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<JwtTokenService>();
        services.AddScoped<ITenantSwitchService, TenantSwitchService>();

        // ── Membership + roles ────────────────────────────────────────────────

        services.AddScoped<IMembershipService, MembershipService>();
        services.AddScoped<IRoleService, RoleService>();

        // ── Permission system ─────────────────────────────────────────────────

        // MemoryDistributedCache default — hosts may replace with Redis impl via DI.
        services.AddDistributedMemoryCache();

        services.AddSingleton<IPermissionDefinitionProvider, HostPermissionProvider>();
        services.AddSingleton<PermissionDefinitionManager>();
        services.AddSingleton<IPermissionGrantCache, DistributedPermissionGrantCache>();
        services.AddScoped<IPermissionGrantRepository, EfCorePermissionGrantRepository>();
        services.AddScoped<IImpersonationSessionRepository, EfCoreImpersonationSessionRepository>();
        services.AddSingleton<IJtiBlacklist, RedisJtiBlacklist>();
        services.AddScoped<IImpersonationRateLimiter, RedisImpersonationRateLimiter>();
        services.AddScoped<IImpersonationService, ImpersonationService>();
        services.AddHostedService<ImpersonationStartupValidator>();
        services.AddScoped<IPermissionChecker, PermissionChecker>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // ── Auth HTTP surface + role templates ────────────────────────────────

        services.AddNacAuthHttp();
        services.AddNacRoleTemplates();

        return services;
    }
}
