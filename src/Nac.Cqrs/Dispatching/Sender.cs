using System.Collections.Frozen;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Nac.Cqrs.Pipeline;

namespace Nac.Cqrs.Dispatching;

/// <summary>
/// Abstract base for type-erased handler wrappers stored in the frozen registry.
/// Allows the dispatcher to invoke handlers without knowing concrete request/response
/// types at compile time.
/// </summary>
internal abstract class RequestHandlerBase  // Internal: only used by framework
{
    /// <summary>
    /// Resolves the handler and pipeline behaviors from DI, composes the pipeline,
    /// and executes it for the given <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The raw (boxed) request object.</param>
    /// <param name="sp">Service provider scoped to the current request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The boxed response value.</returns>
    internal abstract ValueTask<object?> HandleAsync(
        object request,
        IServiceProvider sp,
        CancellationToken ct);
}

/// <summary>
/// Strongly-typed handler wrapper. One instance is created per request type at startup
/// and reused for every dispatch of that type.
/// <para>
/// Resolution strategy: the handler interface type is captured at construction and
/// resolved from DI at dispatch time, avoiding compile-time generic constraint violations
/// while preserving full type safety at runtime.
/// </para>
/// </summary>
/// <typeparam name="TRequest">The concrete request type.</typeparam>
/// <typeparam name="TResponse">The response type produced by the handler.</typeparam>
internal sealed class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerBase
    where TRequest : IBaseRequest<TResponse>
{
    // Closed handler interface type registered in DI (e.g. ICommandHandler<MyCmd, MyResult>).
    private readonly Type _handlerServiceType;

    // Cached open-instance delegate: (handlerObj, request, ct) → ValueTask<TResponse>.
    // Built once via reflection to avoid per-call MethodInfo.Invoke overhead.
    private readonly Func<object, TRequest, CancellationToken, ValueTask<TResponse>> _handlerInvoker;

    /// <summary>
    /// Creates the wrapper, capturing the DI service type and building the invocation delegate.
    /// </summary>
    /// <param name="handlerServiceType">
    /// The closed generic handler interface type registered in DI.
    /// </param>
    internal RequestHandlerWrapper(Type handlerServiceType)
    {
        _handlerServiceType = handlerServiceType;
        _handlerInvoker = BuildInvoker(handlerServiceType);
    }

    /// <inheritdoc/>
    internal override async ValueTask<object?> HandleAsync(
        object request,
        IServiceProvider sp,
        CancellationToken ct)
    {
        var typedRequest = (TRequest)request;

        // Resolve the terminal handler from DI.
        var handlerInstance = sp.GetRequiredService(_handlerServiceType);

        // Build innermost pipeline step: the handler itself.
        RequestHandlerDelegate<TResponse> pipeline =
            () => _handlerInvoker(handlerInstance, typedRequest, ct);

        // Compose behaviors in reverse registration order so the first registered
        // behavior is outermost (wraps all subsequent ones).
        var behaviors = sp
            .GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .Reverse();

        foreach (var behavior in behaviors)
        {
            var next = pipeline;
            var captured = behavior;
            pipeline = () => captured.HandleAsync(typedRequest, next, ct);
        }

        var result = await pipeline().ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Uses reflection to build a strongly-typed delegate that calls
    /// <c>HandleAsync(TRequest, CancellationToken)</c> on whatever handler object is
    /// passed in, avoiding <see cref="MethodInfo.Invoke"/> boxing on every request.
    /// </summary>
    private static Func<object, TRequest, CancellationToken, ValueTask<TResponse>> BuildInvoker(
        Type handlerServiceType)
    {
        // The interface declares exactly one method: HandleAsync(TRequest, CancellationToken).
        var method = handlerServiceType.GetMethod(
            "HandleAsync",
            BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException(
                $"Could not find 'HandleAsync' on '{handlerServiceType.FullName}'.");

        // Create an open delegate: first param is the handler instance (typed as object
        // to avoid the generic constraint problem), remaining params match the signature.
        var handlerParam = System.Linq.Expressions.Expression.Parameter(typeof(object), "handler");
        var requestParam = System.Linq.Expressions.Expression.Parameter(typeof(TRequest), "request");
        var ctParam = System.Linq.Expressions.Expression.Parameter(typeof(CancellationToken), "ct");

        var call = System.Linq.Expressions.Expression.Call(
            System.Linq.Expressions.Expression.Convert(handlerParam, handlerServiceType),
            method,
            requestParam,
            ctParam);

        return System.Linq.Expressions.Expression
            .Lambda<Func<object, TRequest, CancellationToken, ValueTask<TResponse>>>(
                call, handlerParam, requestParam, ctParam)
            .Compile();
    }
}

/// <summary>
/// Default <see cref="ISender"/> implementation backed by a
/// <see cref="FrozenDictionary{TKey,TValue}"/> for O(1) handler lookup.
/// The dictionary is built once at startup; consumers depend on <see cref="ISender"/>.
/// </summary>
internal sealed class Sender : ISender
{
    private readonly FrozenDictionary<Type, RequestHandlerBase> _handlers;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes the sender with the pre-built handler registry and a service provider.
    /// </summary>
    /// <param name="handlers">Frozen map of request type → handler wrapper.</param>
    /// <param name="serviceProvider">DI container used to resolve per-dispatch services.</param>
    public Sender(
        FrozenDictionary<Type, RequestHandlerBase> handlers,
        IServiceProvider serviceProvider)
    {
        _handlers = handlers;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public async ValueTask<TResponse> SendAsync<TResponse>(
        IBaseRequest<TResponse> request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();

        if (!_handlers.TryGetValue(requestType, out var wrapper))
        {
            throw new InvalidOperationException(
                $"No handler registered for request type '{requestType.FullName}'. " +
                "Ensure the handler's assembly was passed to RegisterHandlersFromAssembly().");
        }

        var result = await wrapper.HandleAsync(request, _serviceProvider, ct)
            .ConfigureAwait(false);

        // Guaranteed non-null for all value types (including Unit); safe cast.
        return (TResponse)result!;
    }
}
