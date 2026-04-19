using Nac.Cqrs;
using Nac.Cqrs.Dispatching;

namespace Nac.Testing.Fakes;

public sealed class FakeSender : ISender
{
    private readonly Dictionary<Type, object> _responses = [];
    public List<object> SentRequests { get; } = [];

    public void Setup<TRequest, TResponse>(TResponse response)
        where TRequest : IBaseRequest<TResponse>
    {
        _responses[typeof(TRequest)] = response!;
    }

    public ValueTask<TResponse> SendAsync<TResponse>(
        IBaseRequest<TResponse> request, CancellationToken ct = default)
    {
        SentRequests.Add(request);
        var requestType = request.GetType();

        if (_responses.TryGetValue(requestType, out var response))
            return new ValueTask<TResponse>((TResponse)response);

        throw new InvalidOperationException(
            $"No response configured for {requestType.Name}. Call Setup<{requestType.Name}, TResponse>(response) first.");
    }
}
