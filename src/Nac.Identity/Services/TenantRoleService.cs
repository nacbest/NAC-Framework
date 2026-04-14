using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nac.Identity.Data;
using Nac.Identity.Entities;
using Nac.Identity.Options;
using Nac.Identity.Seeding;

namespace Nac.Identity.Services;

/// <summary>
/// Implementation of tenant role management.
/// </summary>
public sealed class TenantRoleService : ITenantRoleService
{
    private readonly NacIdentityDbContext _dbContext;
    private readonly NacIdentityOptions _options;

    public TenantRoleService(
        NacIdentityDbContext dbContext,
        IOptions<NacIdentityOptions> options)
    {
        _dbContext = dbContext;
        _options = options.Value;
    }

    public async Task InitializeTenantAsync(string tenantId, Guid ownerUserId)
    {
        // Get role definitions (custom or default)
        var roleDefinitions = _options.DefaultRoles.Count > 0
            ? _options.DefaultRoles.Select(d => new DefaultRoleDefinition
            {
                Name = d.Name,
                Permissions = d.Permissions
            }).ToList()
            : DefaultRoles.GetDefaults().ToList();

        Guid? ownerRoleId = null;

        foreach (var def in roleDefinitions)
        {
            // Check if role already exists
            var existing = await _dbContext.TenantRoles
                .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Name == def.Name);

            if (existing is not null)
            {
                if (def.Name == DefaultRoles.Owner)
                    ownerRoleId = existing.Id;
                continue;
            }

            var role = new TenantRole
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = def.Name,
                Permissions = [.. def.Permissions],
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.TenantRoles.Add(role);

            if (def.Name == DefaultRoles.Owner)
                ownerRoleId = role.Id;
        }

        await _dbContext.SaveChangesAsync();

        // Assign owner user to Owner role
        if (ownerRoleId.HasValue)
        {
            var existingMembership = await _dbContext.TenantMemberships
                .FirstOrDefaultAsync(m => m.UserId == ownerUserId && m.TenantId == tenantId);

            if (existingMembership is null)
            {
                _dbContext.TenantMemberships.Add(new TenantMembership
                {
                    Id = Guid.NewGuid(),
                    UserId = ownerUserId,
                    TenantId = tenantId,
                    TenantRoleId = ownerRoleId.Value,
                    IsOwner = true,
                    JoinedAt = DateTimeOffset.UtcNow
                });

                await _dbContext.SaveChangesAsync();
            }
        }
    }

    public async Task<IReadOnlyList<TenantRole>> GetRolesAsync(string tenantId)
    {
        return await _dbContext.TenantRoles
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<TenantRole?> GetRoleByNameAsync(string tenantId, string roleName)
    {
        return await _dbContext.TenantRoles
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Name == roleName);
    }

    public async Task<TenantRole> CreateRoleAsync(
        string tenantId,
        string name,
        IEnumerable<string> permissions)
    {
        var role = new TenantRole
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Permissions = [.. permissions],
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.TenantRoles.Add(role);
        await _dbContext.SaveChangesAsync();

        return role;
    }

    public async Task UpdateRolePermissionsAsync(
        Guid roleId,
        IEnumerable<string> permissions)
    {
        var role = await _dbContext.TenantRoles.FindAsync(roleId)
            ?? throw new InvalidOperationException($"Role {roleId} not found");

        role.Permissions = [.. permissions];
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> DeleteRoleAsync(Guid roleId)
    {
        // Check if any users assigned
        var hasMembers = await _dbContext.TenantMemberships
            .AnyAsync(m => m.TenantRoleId == roleId);

        if (hasMembers)
            return false;

        var role = await _dbContext.TenantRoles.FindAsync(roleId);
        if (role is not null)
        {
            _dbContext.TenantRoles.Remove(role);
            await _dbContext.SaveChangesAsync();
        }

        return true;
    }

    public async Task<TenantMembership> AssignUserToTenantAsync(
        Guid userId,
        string tenantId,
        Guid roleId,
        bool isOwner = false)
    {
        // Check if already a member
        var existing = await _dbContext.TenantMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.TenantId == tenantId);

        if (existing is not null)
            throw new InvalidOperationException(
                $"User {userId} is already a member of tenant {tenantId}");

        var membership = new TenantMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            TenantRoleId = roleId,
            IsOwner = isOwner,
            JoinedAt = DateTimeOffset.UtcNow
        };

        _dbContext.TenantMemberships.Add(membership);
        await _dbContext.SaveChangesAsync();

        return membership;
    }

    public async Task ChangeUserRoleAsync(
        Guid userId,
        string tenantId,
        Guid newRoleId)
    {
        var membership = await _dbContext.TenantMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.TenantId == tenantId)
            ?? throw new InvalidOperationException(
                $"User {userId} is not a member of tenant {tenantId}");

        membership.TenantRoleId = newRoleId;
        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveUserFromTenantAsync(Guid userId, string tenantId)
    {
        var membership = await _dbContext.TenantMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.TenantId == tenantId);

        if (membership is not null)
        {
            _dbContext.TenantMemberships.Remove(membership);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<TenantMembership?> GetMembershipAsync(Guid userId, string tenantId)
    {
        return await _dbContext.TenantMemberships
            .Include(m => m.TenantRole)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.TenantId == tenantId);
    }
}
