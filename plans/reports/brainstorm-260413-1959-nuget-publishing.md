# Brainstorm Report: NuGet Publishing Setup

**Date:** 2026-04-13  
**Status:** Approved  
**Approach:** Simple Tag-based Workflow

---

## Problem Statement

Hiện tại NAC framework đóng gói local (`/nupkgs/`) và CLI local. Cần chuyển sang publish public lên NuGet.org sử dụng GitHub Actions.

## Requirements

- Package naming: `Nac.*` (giữ nguyên)
- GitHub: Public repository
- Release strategy: Tag-based (push `v*` tag → auto publish)
- CI: Build → Test → Pack → Publish
- CLI tool: Publish cùng workflow với library packages
- Pre-release: Không cần

## Evaluated Approaches

### Approach 1: Simple Tag-based (Selected)
- 1 workflow file duy nhất
- Trigger: `push tags: ['v*']`
- All 15 packages cùng version, publish đồng thời
- **Pros:** Đơn giản, dễ maintain
- **Cons:** Không publish từng package riêng

### Approach 2: Reusable + Manual Dispatch
- Tag-based + manual trigger từ UI
- **Pros:** Linh hoạt hơn
- **Cons:** Phức tạp hơn, cần quản lý version consistency

### Approach 3: Release Please
- Full automation với conventional commits
- **Pros:** Auto changelog, auto version bump
- **Cons:** Overkill cho project này

## Final Solution

### Workflow Design

```
git tag v1.1.0 → GitHub Actions → Build → Test → Pack → Publish (15 packages)
```

### Implementation Tasks

1. Tạo GitHub public repository
2. Tạo NuGet.org account + API key
3. Add `NUGET_API_KEY` GitHub secret
4. Tạo `.github/workflows/publish.yml`
5. Update `Directory.Build.props` (RepositoryUrl, PackageProjectUrl)
6. Update README (NuGet badges)

### Workflow File

```yaml
name: Publish to NuGet
on:
  push:
    tags: ['v*']

jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: echo "VERSION=${GITHUB_REF#refs/tags/v}" >> $GITHUB_ENV
      - run: dotnet restore
      - run: dotnet build -c Release --no-restore
      - run: dotnet test -c Release --no-build
      - run: dotnet pack -c Release -p:Version=$VERSION -o ./nupkgs --no-build
      - run: dotnet nuget push ./nupkgs/*.nupkg -k ${{ secrets.NUGET_API_KEY }} -s https://api.nuget.org/v3/index.json --skip-duplicate
```

### Package Metadata

```xml
<PropertyGroup>
  <RepositoryUrl>https://github.com/USERNAME/NAC</RepositoryUrl>
  <RepositoryType>git</RepositoryType>
  <PackageProjectUrl>https://github.com/USERNAME/NAC</PackageProjectUrl>
</PropertyGroup>
```

## Risk Assessment

| Risk | Mitigation |
|------|------------|
| API key leak | GitHub secret |
| Test fail | Pipeline blocks publish |
| Package name taken | Check trước khi setup |
| Version mismatch | Extract từ git tag |

## Success Criteria

- [ ] GitHub repo created và code pushed
- [ ] NuGet.org account ready với API key
- [ ] GitHub Actions workflow hoạt động
- [ ] Push tag `v1.0.0` → 15 packages published
- [ ] `dotnet add package Nac.Abstractions` hoạt động

## Next Steps

Tạo implementation plan chi tiết với `/ck:plan`.
