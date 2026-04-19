namespace Nac.Cqrs;

/// <summary>
/// Base marker interface for all CQRS requests (commands and queries).
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the request handler.</typeparam>
public interface IBaseRequest<TResponse>;
