using Microsoft.Extensions.DependencyInjection;
using Nac.Abstractions.Messaging;
using Nac.Mediator.Abstractions;
using Nac.Mediator.Core;

namespace Nac.Mediator.Internal;

/// <summary>
/// Wraps dispatch for ICommand&lt;TResult&gt; — resolves handler, builds behavior pipeline,
/// runs pre/post processors, and returns the result as object.
/// </summary>
internal sealed class CommandWrapper<TCommand, TResult> : RequestWrapperBase
    where TCommand : ICommand<TResult>
{
    public override async Task<object?> HandleAsync(object request, IServiceProvider sp, CancellationToken ct)
    {
        var command = (TCommand)request;
        var handler = sp.GetRequiredService<ICommandHandler<TCommand, TResult>>();
        var behaviors = sp.GetServices<ICommandBehavior<TCommand, TResult>>().ToList();
        var preProcessors = sp.GetServices<IPreProcessor<TCommand>>().ToList();
        var postProcessors = sp.GetServices<IPostProcessor<TCommand, TResult>>().ToList();

        // Inner pipeline: pre-processors → handler → post-processors
        RequestHandlerDelegate<TResult> innerPipeline = async ct2 =>
        {
            foreach (var pre in preProcessors)
                await pre.ProcessAsync(command, ct2);

            var result = await handler.HandleAsync(command, ct2);

            foreach (var post in postProcessors)
                await post.ProcessAsync(command, result, ct2);

            return result;
        };

        // Wrap with behaviors (first registered = outermost)
        var pipeline = innerPipeline;
        for (var i = behaviors.Count - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var next = pipeline;
            pipeline = ct2 => behavior.HandleAsync(command, next, ct2);
        }

        return await pipeline(ct);
    }
}
