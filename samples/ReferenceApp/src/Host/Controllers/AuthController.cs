using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
    /// <summary>Register a new user.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Email and Password are required." });

        var tenantId = request.TenantId ?? "default";
        var user = new NacUser(request.Email, tenantId)
        {
            FullName = request.FullName
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { userId = user.Id, email = user.Email, tenantId = user.TenantId });
    }

    /// <summary>Authenticate and return a signed JWT.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Email and Password are required." });

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized(new { error = "Invalid credentials." });

        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
            return Unauthorized(new { error = "Invalid credentials." });

        var token = await jwtTokenService.GenerateTokenAsync(user);
        return Ok(new { token, userId = user.Id, email = user.Email, tenantId = user.TenantId });
    }
}

/// <summary>Register request payload.</summary>
public sealed record RegisterRequest(string Email, string Password, string? FullName = null, string? TenantId = null);

/// <summary>Login request payload.</summary>
public sealed record LoginRequest(string Email, string Password);
