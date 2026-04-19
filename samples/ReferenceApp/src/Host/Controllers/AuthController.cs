using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Nac.Identity.Jwt;
using Nac.Identity.Users;

namespace ReferenceApp.Host.Controllers;

/// <summary>
/// Minimal auth endpoints: register + login.
/// Framework (Nac.Identity) provides no built-in controllers, so the host owns these.
/// Uses UserManager&lt;NacUser&gt; for user management and JwtTokenService for token generation.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<NacUser> userManager,
    JwtTokenService jwtTokenService) : ControllerBase
{
    /// <summary>Register a new global user (Pattern A: no tenant assigned here).</summary>
    [HttpPost("register")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Email and Password are required." });

        var user = new NacUser(request.Email, request.FullName);

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { userId = user.Id, email = user.Email });
    }

    /// <summary>Authenticate and return a tenantless JWT (use /auth/switch-tenant to scope).</summary>
    [HttpPost("login")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Email and Password are required." });

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { error = "Invalid credentials." });

        // Tenantless token on login — client selects tenant via POST /auth/switch-tenant.
        var token = jwtTokenService.GenerateToken(user.Id, null, user.Email!, user.FullName, [], user.IsHost);
        return Ok(new { token, userId = user.Id, email = user.Email });
    }
}

/// <summary>Register request payload.</summary>
public sealed record RegisterRequest(string Email, string Password, string? FullName = null);

/// <summary>Login request payload.</summary>
public sealed record LoginRequest(string Email, string Password);
