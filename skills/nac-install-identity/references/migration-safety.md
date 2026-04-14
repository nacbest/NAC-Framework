# Migration Safety Protocol

<HARD-GATE>
Database migrations MUST be confirmed by user before applying.
NEVER skip this confirmation step.
NEVER auto-apply migrations.
</HARD-GATE>

---

## Migration Workflow

```
┌─────────────────────────────────────────────────────────────┐
│                Migration Safety Protocol                     │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Step 1: Create Migration Files                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ dotnet ef migrations add InitialIdentity            │   │
│  │   -p src/Nac.Identity                               │   │
│  │   -s src/{Namespace}.Host                           │   │
│  └─────────────────────────────────────────────────────┘   │
│                         │                                   │
│                         ▼                                   │
│  Step 2: Generate SQL Preview                               │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ dotnet ef migrations script                         │   │
│  │   -p src/Nac.Identity                               │   │
│  │   -s src/{Namespace}.Host                           │   │
│  │   --idempotent                                      │   │
│  └─────────────────────────────────────────────────────┘   │
│                         │                                   │
│                         ▼                                   │
│  Step 3: Show Confirmation Dialog                           │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ AskUserQuestion with SQL preview                    │   │
│  │ Options: [Apply] [Skip]                             │   │
│  └─────────────────────────────────────────────────────┘   │
│                         │                                   │
│           ┌─────────────┴─────────────┐                    │
│           ▼                           ▼                    │
│  ┌─────────────┐            ┌─────────────────┐           │
│  │ User: Apply │            │ User: Skip      │           │
│  └──────┬──────┘            └────────┬────────┘           │
│         │                            │                     │
│         ▼                            ▼                     │
│  ┌──────────────────┐     ┌────────────────────────┐      │
│  │ database update  │     │ Inform: Run manually   │      │
│  │ Report success   │     │ dotnet ef db update    │      │
│  └──────────────────┘     └────────────────────────┘      │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## AskUserQuestion Template

Use this exact format for migration confirmation:

```json
{
  "questions": [{
    "question": "Apply database migration? This will create Identity tables.",
    "header": "Database Migration",
    "options": [
      {
        "label": "Yes, apply migration",
        "description": "Creates: AspNetUsers, AspNetRoles, TenantRoles, TenantMemberships, RefreshTokens"
      },
      {
        "label": "No, skip for now",
        "description": "Migration files created. Run manually: dotnet ef database update"
      }
    ],
    "multiSelect": false
  }]
}
```

---

## SQL Preview Format

When showing SQL preview to user, include:

1. **Tables being created** (list names)
2. **Key columns** (ID types, important fields)
3. **Indexes and constraints**
4. **Truncate if > 50 lines** (show summary)

### Example Preview

```sql
-- Identity Tables to be created:
-- 1. AspNetUsers      (Identity users - UUID primary key)
-- 2. AspNetRoles      (System roles)
-- 3. AspNetUserRoles  (User-Role mapping)
-- 4. AspNetUserClaims (User claims)
-- 5. AspNetUserLogins (External logins)
-- 6. AspNetUserTokens (User tokens)

-- Nac.Identity Tables:
-- 7. TenantRoles       (Tenant-scoped roles with permissions)
-- 8. TenantMemberships (User-Tenant-Role links)
-- 9. RefreshTokens     (JWT refresh tokens)

CREATE TABLE "AspNetUsers" (
    "Id" uuid NOT NULL,
    "DisplayName" varchar(256),
    "Email" varchar(256),
    "NormalizedEmail" varchar(256),
    "PasswordHash" text,
    "SecurityStamp" text,
    "ConcurrencyStamp" text,
    "PhoneNumber" varchar(20),
    "EmailConfirmed" boolean NOT NULL DEFAULT false,
    "LockoutEnd" timestamptz,
    "LockoutEnabled" boolean NOT NULL DEFAULT true,
    "AccessFailedCount" integer NOT NULL DEFAULT 0,
    CONSTRAINT "PK_AspNetUsers" PRIMARY KEY ("Id")
);

CREATE TABLE "TenantRoles" (
    "Id" uuid NOT NULL,
    "TenantId" varchar(64) NOT NULL,
    "Name" varchar(64) NOT NULL,
    "Permissions" jsonb NOT NULL DEFAULT '[]',
    "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz,
    CONSTRAINT "PK_TenantRoles" PRIMARY KEY ("Id")
);

CREATE TABLE "TenantMemberships" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "TenantId" varchar(64) NOT NULL,
    "TenantRoleId" uuid NOT NULL,
    "IsOwner" boolean NOT NULL DEFAULT false,
    "JoinedAt" timestamptz NOT NULL,
    CONSTRAINT "PK_TenantMemberships" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TenantMemberships_Users" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id"),
    CONSTRAINT "FK_TenantMemberships_Roles" FOREIGN KEY ("TenantRoleId") REFERENCES "TenantRoles"("Id")
);

CREATE TABLE "RefreshTokens" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Token" varchar(256) NOT NULL,
    "TenantId" varchar(64),
    "ExpiresAt" timestamptz NOT NULL,
    "RevokedAt" timestamptz,
    "CreatedAt" timestamptz NOT NULL,
    CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RefreshTokens_Users" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id")
);

-- Indexes
CREATE UNIQUE INDEX "IX_AspNetUsers_Email" ON "AspNetUsers"("NormalizedEmail");
CREATE INDEX "IX_TenantRoles_TenantId" ON "TenantRoles"("TenantId");
CREATE INDEX "IX_TenantMemberships_UserId" ON "TenantMemberships"("UserId");
CREATE INDEX "IX_TenantMemberships_TenantId" ON "TenantMemberships"("TenantId");
CREATE UNIQUE INDEX "IX_RefreshTokens_Token" ON "RefreshTokens"("Token");

-- Full script: ~150 lines
```

---

## Error Handling

### Migration Creation Fails

```markdown
If `dotnet ef migrations add` fails:

1. Check EF tools installed:
   dotnet tool list -g | grep dotnet-ef

2. Install if missing:
   dotnet tool install -g dotnet-ef

3. Check DbContext configuration in Nac.Identity

4. Verify connection string format

5. Report error message to user

6. DO NOT proceed to apply step
```

### Database Update Fails

```markdown
If `dotnet ef database update` fails:

1. Report full error message

2. Common issues:
   - Connection string invalid → Check format, credentials
   - Database doesn't exist → Create with: CREATE DATABASE {name};
   - Permission denied → Grant CREATE, ALTER permissions
   - Table already exists → Migration out of sync

3. Suggest rollback if needed:
   dotnet ef migrations remove -p src/Nac.Identity -s src/{Namespace}.Host
```

---

## Rollback Commands

Always inform user about rollback options after migration:

### Remove Last Migration (if not applied)

```bash
dotnet ef migrations remove \
  -p src/Nac.Identity \
  -s src/{Namespace}.Host
```

### Revert to Previous Migration

```bash
dotnet ef database update {PreviousMigrationName} \
  -p src/Nac.Identity \
  -s src/{Namespace}.Host
```

### Drop All Identity Tables (DESTRUCTIVE)

Only if complete reset needed:

```sql
-- WARNING: This deletes all identity data!
DROP TABLE IF EXISTS "RefreshTokens";
DROP TABLE IF EXISTS "TenantMemberships";
DROP TABLE IF EXISTS "TenantRoles";
DROP TABLE IF EXISTS "AspNetUserTokens";
DROP TABLE IF EXISTS "AspNetUserLogins";
DROP TABLE IF EXISTS "AspNetUserClaims";
DROP TABLE IF EXISTS "AspNetUserRoles";
DROP TABLE IF EXISTS "AspNetRoles";
DROP TABLE IF EXISTS "AspNetUsers";
DROP TABLE IF EXISTS "__EFMigrationsHistory";
```

---

## Checklist Before Apply

Before confirming migration, verify:

- [ ] Database server is running
- [ ] Connection string is correct
- [ ] User has CREATE TABLE permission
- [ ] No conflicting tables exist
- [ ] Backup exists (for production)

---

## Post-Migration Verification

After successful migration:

```bash
# List created tables
psql -d {database} -c "\dt"

# Verify Identity tables
psql -d {database} -c "SELECT COUNT(*) FROM \"AspNetUsers\";"
```
