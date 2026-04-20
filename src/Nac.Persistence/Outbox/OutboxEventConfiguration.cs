using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nac.Persistence.Outbox;

/// <summary>
/// EF Core fluent configuration for the <see cref="OutboxEvent"/> entity.
/// Mapped to <c>__outbox_events</c> so the table is visually separated from business tables.
/// </summary>
internal sealed class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ToTable("__outbox_events");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.EventType)
            .IsRequired()
            .HasMaxLength(512);

        // Payload is arbitrary JSON — no length cap enforced at the DB column level.
        builder.Property(o => o.Payload)
            .IsRequired();

        builder.Property(o => o.Error)
            .HasMaxLength(4000);

        // ── Audit / impersonation context ────────────────────────────────────
        builder.Property(o => o.TenantId)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(o => o.ActorUserId)
            .IsRequired(false);

        builder.Property(o => o.ImpersonatorUserId)
            .IsRequired(false);

        // Composite index for polling query: WHERE ProcessedAt IS NULL ORDER BY CreatedAt
        builder.HasIndex(o => new { o.ProcessedAt, o.CreatedAt });
    }
}
