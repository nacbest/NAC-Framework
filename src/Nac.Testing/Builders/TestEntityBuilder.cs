namespace Nac.Testing.Builders;

/// <summary>
/// Fluent builder base for creating test entities via reflection.
/// </summary>
public abstract class TestEntityBuilder<TEntity, TBuilder>
    where TBuilder : TestEntityBuilder<TEntity, TBuilder>
{
    private readonly Dictionary<string, object?> _properties = [];

    protected TBuilder Self => (TBuilder)this;

    public TBuilder WithProperty(string name, object? value)
    {
        _properties[name] = value;
        return Self;
    }

    public TEntity Build()
    {
        var entity = CreateEntity();
        ApplyProperties(entity);
        return entity;
    }

    protected abstract TEntity CreateEntity();

    private void ApplyProperties(TEntity entity)
    {
        var type = typeof(TEntity);
        foreach (var (name, value) in _properties)
        {
            var prop = type.GetProperty(name);
            if (prop is { CanWrite: true })
                prop.SetValue(entity, value);
        }
    }
}
