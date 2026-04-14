# NAC CLI Swagger Auto-Setup & Welcome Landing Page

**Date**: 2026-04-13
**Severity**: Low
**Component**: CLI code generation, API documentation
**Status**: Resolved

## What Happened

Successfully enhanced NAC CLI to auto-generate Swagger/OpenAPI support in new projects and created a minimalist welcome landing page.

## The Brutal Truth

This was straightforward. No surprises, no firefighting. Just solid implementation that reduces boilerplate for developers spinning up new APIs.

## Technical Details

**Changes:**
- Modified `CodeTemplates.cs`: Added Swashbuckle.AspNetCore NuGet package to generated Host csproj
- Enhanced `Program.cs` template: Swagger services/middleware registered (dev-only to avoid production bloat)
- Created welcome endpoint at `/` returning dark/light-aware HTML with: API metadata (name, version, env, uptime), quick links to Swagger/health/spec
- Fixed XSI vulnerability: Regex sanitization on project name in HTML output
- Fixed uptime calculation: Changed `Hours` to `TotalHours` to properly display multi-day uptimes

## Lessons Learned

Small quality-of-life improvements compound. Developers get instant API documentation without manual Swagger setup. The minimalist landing page serves as entry point without friction.

Sanitize all user input before HTML rendering—even project names can break escaping.

## Next Steps

Monitor usage. Gather feedback on landing page UX.
