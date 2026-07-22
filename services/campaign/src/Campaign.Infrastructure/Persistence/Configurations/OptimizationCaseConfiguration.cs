using Campaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Campaign.Infrastructure.Persistence.Configurations;

public class OptimizationCaseConfiguration : IEntityTypeConfiguration<OptimizationCase>
{
    public void Configure(EntityTypeBuilder<OptimizationCase> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.CaseNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(c => c.CaseNumber).IsUnique();

        builder.Property(c => c.Segment).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.Priority).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(25).IsRequired();
        builder.Property(c => c.SlaBreached).HasDefaultValue(false);
        builder.Property(c => c.ConversionLift).HasPrecision(4, 2);
        builder.Property(c => c.SlaDeadline).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();

        // Sorgu desenleri: dashboard (durum+oncelik) ve uzman paneli (uzman+durum)
        builder.HasIndex(c => new { c.Status, c.Priority });
        builder.HasIndex(c => new { c.AssignedExpertId, c.Status });

        builder.HasOne(c => c.Campaign)
            .WithMany()
            .HasForeignKey(c => c.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
