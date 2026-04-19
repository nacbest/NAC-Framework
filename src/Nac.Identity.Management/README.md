# Nac.Identity.Management

Admin REST API module for users, memberships, roles, and permission grants. Mirrors the structure of `Nac.MultiTenancy.Management`. All endpoints are scoped to the authenticated caller's current tenant and gated by `Identity.Management.*` permissions evaluated via `IPermissionChecker` (not JWT claims).

## Endpoints

### Users — `GET /api/identity/users`

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| GET | `/api/identity/users` | `Users.View` | List users with active membership in current tenant |
| GET | `/api/identity/users/{id}` | `Users.View` | User detail + tenant memberships |
| POST | `/api/identity/users/{id}/deactivate` | `Users.Manage` | Soft-disable globally (host admin only) |

### Memberships — `GET /api/identity/memberships`

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| POST | `/api/identity/memberships/invite` | `Memberships.Manage` | Invite user to current tenant |
| POST | `/api/identity/memberships/{id}/accept` | authenticated | Accept a pending invitation |
| GET | `/api/identity/memberships` | `Memberships.View` | List memberships in current tenant |
| DELETE | `/api/identity/memberships/{id}` | `Memberships.Manage` | Remove member (soft-delete) |
| PATCH | `/api/identity/memberships/{id}/roles` | `Memberships.Manage` | Replace role assignments |

### Roles — `GET /api/identity/roles`

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| GET | `/api/identity/roles` | `Roles.View` | List tenant roles |
| GET | `/api/identity/roles/{id}` | `Roles.View` | Role detail + grants |
| GET | `/api/identity/role-templates` | `Roles.View` | List system template roles |
| POST | `/api/identity/roles/from-template` | `Roles.Manage` | Clone template into tenant |
| POST | `/api/identity/roles` | `Roles.Manage` | Create custom role |
| PATCH | `/api/identity/roles/{id}` | `Roles.Manage` | Update name/description |
| DELETE | `/api/identity/roles/{id}` | `Roles.Manage` | Soft-delete (409 if referenced) |
| GET | `/api/identity/roles/{id}/grants` | `Grants.View` | List role grants |
| POST | `/api/identity/roles/{id}/grants` | `Grants.Manage` | Grant one permission |
| DELETE | `/api/identity/roles/{id}/grants/{permissionName}` | `Grants.Manage` | Revoke one permission |
| PUT | `/api/identity/roles/{id}/grants` | `Grants.Manage` | Bulk replace (single invalidation) |

### User Direct Grants — `GET /api/identity/users/{userId}/grants`

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| GET | `/api/identity/users/{userId}/grants` | `Grants.View` | List direct grants |
| POST | `/api/identity/users/{userId}/grants` | `Grants.Manage` | Grant one permission |
| DELETE | `/api/identity/users/{userId}/grants/{permissionName}` | `Grants.Manage` | Revoke one permission |

### Permissions — `GET /api/identity/permissions`

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| GET | `/api/identity/permissions` | `Permissions.View` | Full hierarchical permission tree |

## Registration

```csharp
services.AddNacIdentityManagement();
// or via module system:
// [DependsOn(typeof(NacIdentityManagementModule))]
```

Requires `AddNacIdentity<TContext>(...)` to be called first.

## Cache Invalidation

Every mutating endpoint triggers `IPermissionGrantCache.InvalidateAsync`. Bulk grant replace (`PUT /roles/{id}/grants`) performs a **single** invalidation after the full batch to avoid cache storms.
