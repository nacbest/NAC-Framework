namespace Nac.Mediator;

/// <summary>
/// Delegate representing the next step in the command pipeline.
/// Each behavior invokes this to continue the chain, or skips it to short-circuit.
/// </summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken ct);
