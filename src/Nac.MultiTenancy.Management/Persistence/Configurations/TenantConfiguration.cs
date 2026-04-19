using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nac.MultiTenancy.Management.Domain;

namespace Nac.MultiTenancy.Management.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Tenant"/>. Uniqueness is enforced on
/// <c>Identifier</c>; <c>Properties</c> is round-tripped through JSON; an
/// optimistic-concurrency <c>RowVersion</c> shadow property is added.
/// </summary>
internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Tenant> b)
    {
        b.ToTable("Tenants");
        b.HasKey(x => x.Id);

        b.Property(x => x.Identifier).IsRequired().HasMaxLength(50);
        b.HasIndex(x => x.Identifier).IsUnique();

        b.Property(x => x.Name).IsRequired().HasMaxLength(200);

        b.Property(x => x.IsolationMode).HasConversion<int>().IsRequired();

        b.Property(x => x.EncryptedConnectionString).HasMaxLength(4000);

        b.Property(x => x.IsActive).IsRequired();

        // Properties dictionary serialised as JSON column; case-insensitive comparer
        // is restored on materialisation to preserve aggregate invariant.
        var jsonOpts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        b.Property(x => x.Properties)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOpts),
                v => DeserializeProperties(v, jsonOpts))
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking
                .ValueComparer<Dictionary<string, string?>>(
                    (a, b) => DictEquals(a, b),
                    v => v == null ? 0 : v.Aggregate(0, (h, kv) =>
                        HashCode.Combine(h, kv.Key, kv.Value)),
                    v => CloneDict(v)));

        // Audit + soft-delete columns (interfaces from Nac.Core).
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.IsDeleted).IsRequired();
        b.Property(x => x.DeletedAt);

        // Optimistic concurrency token — shadow property provider-mapped to rowversion / xmin.
        b.Property<byte[]>("RowVersion").IsRowVersion();
    }

    private static Dictionary<string, string?> DeserializeProperties(
        string json, JsonSerializerOptions opts)
    {
        if (string.IsNullOrWhiteSpace(json)) return new(StringComparer.OrdinalIgnoreCase);
        var raw = JsonSerializer.Deserialize<Dictionary<string, string?>>(json, opts)
                  ?? new Dictionary<string, string?>();
        return new Dictionary<string, string?>(raw, StringComparer.OrdinalIgnoreCase);
    }

    private static bool DictEquals(
        Dictionary<string, string?>? a, Dictionary<string, string?>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a.Count != b.Count) return false;
        foreach (var kv in a)
        {
            if (!b.TryGetValue(kv.Key, out var v)) return false;
            if (!string.Equals(kv.Value, v, StringComparison.Ordinal)) return false;
        }
        return true;
    }

    private static Dictionary<string, string?> CloneDict(Dictionary<string, string?> v) =>
        new(v, StringComparer.OrdinalIgnoreCase);
}
