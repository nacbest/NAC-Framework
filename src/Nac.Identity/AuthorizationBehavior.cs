using Nac.Core.Auth;
using Nac.Core.Exceptions;
using Nac.CQRS.Abstractions;
using Nac.CQRS.Core;

namespace Nac.Identity;

/// <summary>
/// Command pipeline behavior that enforces permission-based authorization.
/// If the command implements <see cref="IRequirePermission"/>, checks that the
/// current user holds the required permission before invoking the handler.
/// </summary>
public sealed class AuthorizationCommandBehavior<TCommand, TResponse>
    : ICommandBehavior<TCommand, TResponse>
{
    private readonly ICurrentUser _currentUser;

    public AuthorizationCommandBehavior(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public Task<TResponse> HandleAsync(
        TCommand command,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (command is not IRequirePermission secured)
            return next(ct);

        if (!_currentUser.IsAuthenticated)
            throw new NacUnauthorizedException();

        if (!_currentUser.HasPermission(secured.Permission))
            throw new NacForbiddenException(
                $"Permission '{secured.Permission}' is required.");

        return next(ct);
    }
}

/// <summary>
/// Query pipeline behavior that enforces permission-based authorization.
/// Same logic as <see cref="AuthorizationCommandBehavior{TCommand,TResponse}"/>
/// but registered in the query pipeline.
/// </summary>
public sealed class AuthorizationQueryBehavior<TQuery, TResponse>
    : IQueryBehavior<TQuery, TResponse>
{
    private readonly ICurrentUser _currentUser;

    public AuthorizationQueryBehavior(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public Task<TResponse> HandleAsync(
        TQuery query,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (query is not IRequirePermission secured)
            return next(ct);

        if (!_currentUser.IsAuthenticated)
            throw new NacUnauthorizedException();

        if (!_currentUser.HasPermission(secured.Permission))
            throw new NacForbiddenException(
                $"Permission '{secured.Permission}' is required.");

        return next(ct);
    }
}
