namespace Nac.Core.Results;

public sealed record ValidationError(string Identifier, string ErrorMessage);
