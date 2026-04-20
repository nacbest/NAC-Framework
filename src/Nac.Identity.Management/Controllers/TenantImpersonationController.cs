using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nac.Core.Abstractions.Identity;
using Nac.Identity.Impersonation;
using Nac.Identity.Management.Authorization;
using Nac.Identity.Management.Contracts.Impersonation;

namespace Nac.Identity.Management.Controllers;

/// <summary>
/// Host-admin REST API for the impersonation lifecycle.
/// All endpoints require an authenticated host user with the
/// <c>Host.ImpersonateTenant</c> runtime permission (enforced by
/// <see cref="HostImpersonationFilter"/> — Pattern A, NOT claim-based policy).
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize]
[ServiceFilter(typeof(HostImpersonationFilter))]
public sealed class TenantImpersonationController(
    IImpersonationService impersonationService,
    ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// Issues a 15-minute impersonation token scoped to <paramref name="tenantId"/>.
    /// Creates an immutable audit <c>ImpersonationSession</c> row on success.
    /// </summary>
    /// <param name="tenantId">Target tenant slug — authoritative, never taken from request body.</param>
    /// <param name="body">Request containing the operator-supplied reason.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 with <see cref="IssueImpersonationResponse"/> on success.</returns>
    [HttpPost("tenants/{tenantId}/impersonate")]
    [ProducesResponseType(typeof(IssueImpersonationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<IssueImpersonationResponse>> Issue(
        string tenantId,
        [FromBody] IssueImpersonationRequest body,
        CancellationToken ct)
    {
        var result = await impersonationService.IssueAsync(
            currentUser.Id, tenantId, body.Reason, ct);

        return Ok(new IssueImpersonationResponse(
            result.Token,
            result.Session.ExpiresAt,
            result.Session.Id));
    }

    /// <summary>
    /// Lists impersonation sessions for audit purposes (most recent first).
    /// <c>Jti</c> is excluded from each item — it is an internal revocation key.
    /// </summary>
    /// <param name="tenantId">Filter by target tenant slug.</param>
    /// <param name="page">1-based page number (default: 1).</param>
    /// <param name="pageSize">Items per page, 1–100 (default: 20).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 with paged list of <see cref="ImpersonationSessionDto"/>.</returns>
    [HttpGet("impersonation-sessions")]
    [ProducesResponseType(typeof(IReadOnlyList<ImpersonationSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ImpersonationSessionDto>>> List(
        [FromQuery] string tenantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            return BadRequest("tenantId query parameter is required.");

        if (page < 1) page = 1;
        pageSize = Math.Clamp(pageSize, 1, 100);

        var skip = (page - 1) * pageSize;
        var sessions = await impersonationService.ListByTenantAsync(tenantId, skip, pageSize, ct);
        var dtos = sessions.Select(ImpersonationSessionDto.From).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Revokes an active impersonation session by id.
    /// Adds the session's <c>jti</c> to the blacklist and marks the row revoked.
    /// Idempotent — re-revoking an already-revoked session is a no-op.
    /// </summary>
    /// <param name="id">Impersonation session surrogate id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 NoContent on success; 404 if session not found.</returns>
    [HttpPost("impersonation-sessions/{id:guid}/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
    {
        await impersonationService.RevokeAsync(id, currentUser.Id, ct);
        return NoContent();
    }
}
