namespace RentalManager.BuildingBlocks.Tenancy.Cqrs;

public interface ICommand
{
}

public interface ICommand<TResult> : ICommand
{
}
