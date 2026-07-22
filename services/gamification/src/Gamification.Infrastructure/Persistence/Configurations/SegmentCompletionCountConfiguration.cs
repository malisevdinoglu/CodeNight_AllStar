using Gamification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gamification.Infrastructure.Persistence.Configurations;

public class SegmentCompletionCountConfiguration : IEntityTypeConfiguration<SegmentCompletionCount>
{
    public void Configure(EntityTypeBuilder<SegmentCompletionCount> builder)
    {
        builder.ToTable("segment_completion_counts");

        builder.HasKey(c => new { c.ExpertId, c.Segment });
        builder.Property(c => c.Segment).HasMaxLength(20);
        builder.Property(c => c.CompletedCount).HasDefaultValue(0);
    }
}
