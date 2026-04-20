namespace Nac.Identity.Impersonation;

/// <summary>Successful outcome of <see cref="IImpersonationService.IssueAsync"/>.</summary>
public sealed record ImpersonationIssueResult(string Token, ImpersonationSession Session);
