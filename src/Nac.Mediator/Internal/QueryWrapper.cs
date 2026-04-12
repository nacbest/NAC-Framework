using Microsoft.Extensions.DependencyInjection;
using Nac.Messaging;

namespace Nac.Mediator.Internal;

/// <summary>
/// Wraps dispatch for IQuery&lt;TResult&gt; — resolves handler, builds query behavior
/// pipeline (separate from command pipeline), and returns the result.
/// </summary>
internal sealed class QueryWrapper<TQuery, TResult> : RequestWrapperBase
    where TQuery : IQuery<TResult>
{
    public override async Task<object?> HandleAsync(object request, IServiceProvider sp, CancellationToken ct)
    {
        var query = (TQuery)request;
        var handler = sp.GetRequiredService<IQueryHandler<TQuery, TResult>>();
        var behaviors = sp.GetServices<IQueryBehavior<TQuery, TResult>>().ToList();
        var preProcessors = sp.GetServices<IPreProcessor<TQuery>>().ToList();
        var postProcessors = sp.GetServices<IPostProcessor<TQuery, TResult>>().ToList();

        RequestHandlerDelegate<TResult> innerPipeline = async ct2 =>
        {
            foreach (var pre in preProcessors)
                await pre.ProcessAsync(query, ct2);

            var result = await handler.HandleAsync(query, ct2);

            foreach (var post in postProcessors)
                await post.ProcessAsync(query, result, ct2);

            return result;
        };

        var pipeline = innerPipeline;
        for (var i = behaviors.Count - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var next = pipeline;
            pipeline = ct2 => behavior.HandleAsync(query, next, ct2);
        }

        return await pipeline(ct);
    }
}
