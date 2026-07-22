using Campaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Campaign.Infrastructure.Persistence.Configurations;

public class CaseStatusHistoryConfiguration : IEntityTypeConfiguration<CaseStatusHistory>
{
    public void Configure(EntityTypeBuilder<CaseStatusHistory> builder)
    {
        builder.ToTable("case_status_history");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedOnAdd(); // bigserial

        builder.Property(h => h.FromStatus).HasConversion<string>().HasMaxLength(25).IsRequired();
        builder.Property(h => h.ToStatus).HasConversion<string>().HasMaxLength(25).IsRequired();
        builder.Property(h => h.ChangedAt).IsRequired();

        builder.HasIndex(h => h.CaseId);

        builder.HasOne(h => h.Case)
            .WithMany(c => c.StatusHistory)
            .HasForeignKey(h => h.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
