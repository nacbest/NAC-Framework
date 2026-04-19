using System.Security.Cryptography;
using System.Text;

namespace Nac.Identity.RoleTemplates;

/// <summary>
/// Derives a deterministic <see cref="Guid"/> from a role template key so that the
/// seeder can use the same primary key across every application boot (idempotent upsert
/// by PK). Uses MD5 of the UTF-8 encoded key — collision safety is not required here
/// because keys are controlled strings, not user input.
/// </summary>
public static class RoleTemplateKeyHasher
{
    /// <summary>
    /// Returns a stable <see cref="Guid"/> for <paramref name="key"/> by hashing its
    /// UTF-8 bytes with MD5 and reinterpreting the 16-byte output as a <see cref="Guid"/>.
    /// </summary>
    /// <param name="key">Role template key (e.g. "owner", "admin"). Case-sensitive.</param>
    public static Guid ToGuid(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var bytes = Encoding.UTF8.GetBytes(key);
        var hash = MD5.HashData(bytes);
        return new Guid(hash);
    }
}
