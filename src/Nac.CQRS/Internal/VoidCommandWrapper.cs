using Microsoft.Extensions.DependencyInjection;
using Nac.Core.Messaging;
using Nac.CQRS.Abstractions;
using Nac.CQRS.Core;

namespace Nac.CQRS.Internal;

/// <summary>
/// Wraps dispatch for void ICommand — adapts the void handler to return Unit
/// so it can share the same behavior pipeline as result commands.
/// </summary>
internal sealed class VoidCommandWrapper<TCommand> : RequestWrapperBase
    where TCommand : ICommand
{
    public override async Task<object?> HandleAsync(object request, IServiceProvider sp, CancellationToken ct)
    {
        var command = (TCommand)request;
        var handler = sp.GetRequiredService<ICommandHandler<TCommand>>();
        var behaviors = sp.GetServices<ICommandBehavior<TCommand, Unit>>().ToList();
        var preProcessors = sp.GetServices<IPreProcessor<TCommand>>().ToList();
        var postProcessors = sp.GetServices<IPostProcessor<TCommand, Unit>>().ToList();

        RequestHandlerDelegate<Unit> innerPipeline = async ct2 =>
        {
            foreach (var pre in preProcessors)
                await pre.ProcessAsync(command, ct2);

            await handler.HandleAsync(command, ct2);

            foreach (var post in postProcessors)
                await post.ProcessAsync(command, Unit.Value, ct2);

            return Unit.Value;
        };

        var pipeline = innerPipeline;
        for (var i = behaviors.Count - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var next = pipeline;
            pipeline = ct2 => behavior.HandleAsync(command, next, ct2);
        }

        await pipeline(ct);
        return null;
    }
}
