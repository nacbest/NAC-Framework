namespace Nac.Core.Primitives;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
