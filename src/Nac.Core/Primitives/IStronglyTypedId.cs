namespace Nac.Core.Primitives;

public interface IStronglyTypedId<out T> where T : notnull
{
    T Value { get; }
}
