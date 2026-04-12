using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nac.Persistence.Outbox;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Payload).IsRequired();

        // Worker queries by ProcessedAt IS NULL; partial index is ideal but provider-specific
        builder.HasIndex(x => x.ProcessedAt);
    }
}
