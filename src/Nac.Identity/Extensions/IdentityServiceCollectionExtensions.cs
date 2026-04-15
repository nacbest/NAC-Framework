using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nac.Core.Auth;
using Nac.Core.Messaging;
using Nac.Identity.CurrentUser;
using Nac.Identity.Data;
using Nac.Identity.Entities;
using Nac.Identity.MultiTenancy;
using Nac.Identity.Options;
using Nac.Identity.Seeding;
using Nac.Identity.Services;
using Nac.CQRS.Abstractions;
using StackExchange.Redis;

namespace Nac.Identity.Extensions;

/// <summary>
/// DI registration for NAC Identity services.
/// </summary>
public static class IdentityServiceCollectionExtensions
{
    /// <summary>
    /// Adds NAC Identity services with a custom user type, JWT authentication, and multi-tenant permissions.
    /// </summary>
    /// <typeparam name="TUser">Custom user type extending <see cref="NacIdentityUser"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration for binding options.</param>
    /// <param name="configureDbContext">Action to configure the DbContext (e.g., UseNpgsql).</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>Identity builder for further configuration.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddNacIdentity&lt;AppUser&gt;(
    ///     builder.Configuration,
    ///     db => db.UseNpgsql(connectionString)
    /// );
    /// </code>
    /// </example>
    public static IdentityBuilder AddNacIdentity<TUser>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder> configureDbContext,
        Action<NacIdentityOptions>? configure = null)
        where TUser : NacIdentityUser
    {
        // Bind options from configuration
        var optionsSection = configuration.GetSection(NacIdentityOptions.SectionName);
        services.Configure<NacIdentityOptions>(optionsSection);

        // Apply additional configuration
        if (configure is not null)
            services.PostConfigure(configure);

        // Build options for immediate use (signing key validation, Redis setup)
        var options = new NacIdentityOptions();
        optionsSection.Bind(options);
        configure?.Invoke(options);

        // Add Identity DbContext with user-provided configuration
        services.AddDbContext<NacIdentityDbContext<TUser>>(configureDbContext);

        // Add ASP.NET Core Identity
        var identityBuilder = services
            .AddIdentity<TUser, NacRole>(identityOpts =>
            {
                // Password requirements
                identityOpts.Password.RequireDigit = true;
                identityOpts.Password.RequireLowercase = true;
                identityOpts.Password.RequireUppercase = true;
                identityOpts.Password.RequireNonAlphanumeric = false;
                identityOpts.Password.RequiredLength = 8;

                // Lockout settings
                identityOpts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                identityOpts.Lockout.MaxFailedAccessAttempts = 5;

                // User settings
                identityOpts.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<NacIdentityDbContext<TUser>>()
            .AddDefaultTokenProviders();

        // Replace default UserManager with TenantAwareUserManager
        services.AddScoped<UserManager<TUser>, TenantAwareUserManager<TUser>>();

        // Add JWT Authentication
        services.AddAuthentication(authOpts =>
        {
            authOpts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            authOpts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(jwtOpts =>
        {
            var signingKey = options.SigningKey
                ?? configuration["NacIdentity:SigningKey"]
                ?? throw new InvalidOperationException(
                    "NacIdentity:SigningKey must be configured");

            jwtOpts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = options.Issuer,
                ValidAudience = options.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(signingKey)),
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

        // Add HttpContextAccessor (required for JwtCurrentUser)
        services.AddHttpContextAccessor();

        // Register ICurrentUser implementation
        services.AddScoped<ICurrentUser, JwtCurrentUser>();

        // Register token + identity services
        services.AddScoped<IJwtTokenService<TUser>, JwtTokenService<TUser>>();
        services.AddScoped<ITenantRoleService, TenantRoleService>();
        services.AddScoped<IIdentityService, IdentityService<TUser>>();
        services.AddScoped<IdentityEventPublisher<TUser>>();

        // Register refresh token store (EF or Redis)
        if (options.UseRedisRefreshTokenStore && !string.IsNullOrEmpty(options.RedisConnection))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(options.RedisConnection));
            services.AddScoped<IRefreshTokenStore, RedisRefreshTokenStore>();
        }
        else
        {
            services.AddScoped<IRefreshTokenStore, EfRefreshTokenStore>();

            // Add cleanup service for EF store
            services.AddHostedService<RefreshTokenCleanupService>();
        }

        // Register seeder
        services.AddSingleton<IdentitySeeder>();

        // Register authorization behaviors for mediator pipeline
        services.AddNacAuthorization();

        return identityBuilder;
    }

    /// <summary>
    /// Adds NAC Identity services using the default <see cref="NacIdentityUser"/> type.
    /// Also registers the non-generic <see cref="IJwtTokenService"/> and <see cref="IdentityEventPublisher"/> aliases.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration for binding options.</param>
    /// <param name="configureDbContext">Action to configure the DbContext (e.g., UseNpgsql).</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>Identity builder for further configuration.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddNacIdentity(
    ///     builder.Configuration,
    ///     db => db.UseNpgsql(connectionString),
    ///     opts => opts.AccessTokenExpiry = TimeSpan.FromMinutes(30)
    /// );
    /// </code>
    /// </example>
    public static IdentityBuilder AddNacIdentity(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder> configureDbContext,
        Action<NacIdentityOptions>? configure = null)
    {
        var builder = services.AddNacIdentity<NacIdentityUser>(
            configuration, configureDbContext, configure);

        // Register non-generic aliases for backward-compat and convenience
        services.AddScoped<IJwtTokenService>(sp =>
            new JwtTokenService(
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<NacIdentityOptions>>(),
                sp.GetRequiredService<IRefreshTokenStore>(),
                sp.GetRequiredService<UserManager<NacIdentityUser>>()));

        services.AddScoped<IdentityEventPublisher>(sp =>
            new IdentityEventPublisher(sp.GetService<IEventBus>()));

        return builder;
    }

    /// <summary>
    /// Registers authorization behaviors for both command and query pipelines.
    /// Commands/queries implementing <see cref="IRequirePermission"/>
    /// will be checked against the current user's permissions.
    /// Called automatically by <see cref="AddNacIdentity"/>; can also be used standalone.
    /// </summary>
    public static IServiceCollection AddNacAuthorization(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient(
            typeof(ICommandBehavior<,>),
            typeof(AuthorizationCommandBehavior<,>)));

        services.TryAddEnumerable(ServiceDescriptor.Transient(
            typeof(IQueryBehavior<,>),
            typeof(AuthorizationQueryBehavior<,>)));

        return services;
    }
}
