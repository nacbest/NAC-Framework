---
status: pending
priority: high
createdAt: 2026-04-13
brainstorm: ../reports/brainstorm-260413-1959-nuget-publishing.md
---

# NuGet Publishing Setup

> Tag-based GitHub Actions workflow to publish NAC framework (15 packages) to NuGet.org

## Overview

**Goal:** Chuyển từ local packaging sang public NuGet.org với automated CI/CD.

**Approach:** Simple tag-based workflow
- Push `v*` tag → GitHub Actions → Build → Test → Pack → Publish
- All 15 packages cùng version, publish đồng thời

## Prerequisites (Manual Steps)

Các bước này cần user thực hiện thủ công:

### 1. GitHub Repository
```bash
# Tạo repo trên github.com (public)
# Sau đó:
git remote add origin https://github.com/USERNAME/NAC.git
git push -u origin main
```

### 2. NuGet.org Account
1. Đăng ký tại https://www.nuget.org/users/account/LogOn
2. Verify email
3. Tạo API key: Account → API Keys → Create
   - Name: `NAC-GitHub-Actions`
   - Glob pattern: `Nac.*`
   - Scopes: Push

### 3. GitHub Secret
1. Repo → Settings → Secrets → Actions
2. New repository secret:
   - Name: `NUGET_API_KEY`
   - Value: (paste API key from NuGet)

## Implementation Phases

### Phase 1: Create GitHub Actions Workflow
**File:** `.github/workflows/publish.yml`

```yaml
name: Publish to NuGet

on:
  push:
    tags: ['v*']

env:
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true

jobs:
  publish:
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout
        uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Extract version from tag
        run: echo "VERSION=${GITHUB_REF#refs/tags/v}" >> $GITHUB_ENV
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build -c Release --no-restore
      
      - name: Test
        run: dotnet test -c Release --no-build --verbosity normal
      
      - name: Pack
        run: dotnet pack -c Release -p:Version=${{ env.VERSION }} -o ./nupkgs --no-build
      
      - name: Publish to NuGet
        run: dotnet nuget push ./nupkgs/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate
```

### Phase 2: Update Package Metadata
**File:** `Directory.Build.props`

Add RepositoryUrl và PackageProjectUrl:

```xml
<PropertyGroup>
  <!-- Existing -->
  <Authors>NAC Team</Authors>
  <Company>NAC</Company>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <RepositoryType>git</RepositoryType>
  <Version>1.0.0</Version>
  
  <!-- New -->
  <RepositoryUrl>https://github.com/USERNAME/NAC</RepositoryUrl>
  <PackageProjectUrl>https://github.com/USERNAME/NAC</PackageProjectUrl>
</PropertyGroup>
```

### Phase 3: Add CI Build Workflow (Optional but Recommended)
**File:** `.github/workflows/ci.yml`

Workflow chạy trên mỗi PR/push để validate:

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore
      - run: dotnet build -c Release --no-restore
      - run: dotnet test -c Release --no-build
```

### Phase 4: Update README with NuGet Badges
**File:** `README.md`

Add badges và installation instructions:

```markdown
# NAC Framework

[![NuGet](https://img.shields.io/nuget/v/Nac.Abstractions.svg)](https://www.nuget.org/packages/Nac.Abstractions)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Installation

```bash
# Core packages
dotnet add package Nac.Abstractions
dotnet add package Nac.Domain
dotnet add package Nac.Mediator

# Infrastructure
dotnet add package Nac.WebApi
dotnet add package Nac.Persistence
dotnet add package Nac.Persistence.PostgreSQL

# Optional
dotnet add package Nac.Messaging
dotnet add package Nac.Messaging.RabbitMQ
dotnet add package Nac.Caching
dotnet add package Nac.Auth
dotnet add package Nac.MultiTenancy
dotnet add package Nac.Observability
dotnet add package Nac.Testing

# CLI Tool
dotnet tool install -g Nac.Cli
```
```

## Files to Create

| File | Purpose |
|------|---------|
| `.github/workflows/publish.yml` | Tag-based NuGet publish workflow |
| `.github/workflows/ci.yml` | PR/push validation workflow |

## Files to Modify

| File | Change |
|------|--------|
| `Directory.Build.props` | Add RepositoryUrl, PackageProjectUrl |
| `README.md` | Add NuGet badges, installation instructions |

## Checklist

- [ ] GitHub repo created (public)
- [ ] Code pushed to GitHub
- [ ] NuGet.org account created
- [ ] API key generated with `Nac.*` scope
- [ ] `NUGET_API_KEY` secret added to GitHub
- [ ] `.github/workflows/publish.yml` created
- [ ] `.github/workflows/ci.yml` created
- [ ] `Directory.Build.props` updated
- [ ] `README.md` updated with badges

## Testing the Setup

```bash
# After all setup complete:
git tag v1.0.0
git push origin v1.0.0

# Monitor GitHub Actions
# Check NuGet.org after ~5 minutes
# Test installation:
dotnet new console -n TestNac
cd TestNac
dotnet add package Nac.Abstractions --version 1.0.0
```

## Success Criteria

1. Push tag `v1.0.0` triggers GitHub Actions
2. All 15 packages published to NuGet.org
3. `dotnet add package Nac.Abstractions` works
4. `dotnet tool install -g Nac.Cli` works

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| API key leak | GitHub secret, không commit |
| Test fail blocks publish | CI design, fail fast |
| Package name taken | Check availability trước |
| .NET 10 preview | `10.0.x` allows preview versions |
