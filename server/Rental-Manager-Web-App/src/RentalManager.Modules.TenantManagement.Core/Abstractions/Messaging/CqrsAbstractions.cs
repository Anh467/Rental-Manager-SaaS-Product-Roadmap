namespace RentalManager.Modules.TenantManagement.Core.Abstractions.Messaging;

public readonly record struct Unit
{
    public static Unit Value => default;
}

public interface ICommand<out TResult>
{
}

public interface ICommand : ICommand<Unit>
{
}

public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task HandleAsync(
        TCommand command,
        CancellationToken cancellationToken);
}

public interface IQuery<out TResult>
{
}

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(
        TQuery query,
        CancellationToken cancellationToken);
}
