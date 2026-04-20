using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Nac.Identity.Impersonation;
using Nac.Identity.Services;

namespace Nac.Identity.Authentication;

/// <summary>
/// <see cref="JwtBearerEvents.OnTokenValidated"/> hook that rejects impersonation tokens
/// whose <c>jti</c> appears in <see cref="IJtiBlacklist"/>. Non-impersonation tokens are
/// detected by absence of the <c>act</c> claim and skip the check entirely — zero cost
/// for regular bearer traffic.
/// </summary>
internal static class JtiRevocationValidator
{
    public static async Task ValidateAsync(TokenValidatedContext ctx)
    {
        var principal = ctx.Principal;
        if (principal is null) return;
        if (principal.FindFirst(NacIdentityClaims.ActClaim) is null) return;

        var jti = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (string.IsNullOrEmpty(jti))
        {
            ctx.Fail("Impersonation token missing jti.");
            return;
        }

        var blacklist = ctx.HttpContext.RequestServices.GetRequiredService<IJtiBlacklist>();
        if (await blacklist.IsRevokedAsync(jti, ctx.HttpContext.RequestAborted))
            ctx.Fail("Impersonation token revoked.");
    }
}
