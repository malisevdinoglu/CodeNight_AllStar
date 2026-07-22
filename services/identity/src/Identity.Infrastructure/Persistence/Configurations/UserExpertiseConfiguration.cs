using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class UserExpertiseConfiguration : IEntityTypeConfiguration<UserExpertise>
{
    public void Configure(EntityTypeBuilder<UserExpertise> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.SegmentType).HasConversion<string>().HasMaxLength(20).IsRequired();

        // Ayni uzmanlik iki kez atanamaz
        builder.HasIndex(e => new { e.UserId, e.SegmentType }).IsUnique();

        builder.HasOne(e => e.User)
            .WithMany(u => u.Expertises)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
